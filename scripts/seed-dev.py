#!/usr/bin/env python3
"""seed-dev.py — semeia o banco DEV (revoa_dev, instância revoa-mongo-dev).

Contrato do dono: dados de demonstração vivem SEMPRE no banco dev e são
populados ANTES de cada release; o banco de PRODUÇÃO nunca é populado via
script. Este script só enxerga a instância dev — a conexão de produção não
aparece em nenhum comando daqui.

Como funciona (sem código novo no app):
  1. sobe um app EFÊMERO em ASPNETCORE_ENVIRONMENT=Development apontando para
     o banco dev (`docker compose run --rm`), sem exposição pública — só existe
     na rede interna durante o seed;
  2. register → dev-verify (ativa a conta admin e emite JWT com role Admin) →
     POST /api/dev/seed-catalog (gated: Development + Policy=Admin);
  3. remove o container efêmero. O app-dev (Production) serve os dados.

Uso: python scripts/seed-dev.py   (no repo raiz; requer .env com ADMIN_EMAILS)
"""

import io
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SEED_NAME = "revoa-seed"
DEV_MONGO = "mongodb://mongo-dev:27017/?replicaSet=rs0"
DEV_DB = "revoa_dev"


def sh(*args: str, check: bool = True) -> subprocess.CompletedProcess:
    """Roda docker; MSYS_NO_PATHCONV evita o path-mangling do Git Bash."""
    r = subprocess.run(
        ["docker", *args], capture_output=True, text=True, encoding="utf-8",
        errors="replace", env={**__import__("os").environ, "MSYS_NO_PATHCONV": "1"})
    if check and r.returncode != 0:
        sys.exit(f"[seed-dev] falhou: docker {' '.join(args)}\n{r.stdout}\n{r.stderr}")
    return r


def http(method: str, path: str, body: dict | None = None, token: str | None = None) -> tuple[int, str]:
    """HTTP contra o container efêmero via curl interno (nada é publicado no host)."""
    cmd = ["exec", SEED_NAME, "curl", "-s", "-m", "120", "-X", method,
           "-H", "Content-Type: application/json",
           "-w", "\n%{http_code}", f"http://localhost:8000{path}"]
    if token:
        cmd += ["-H", f"Authorization: Bearer {token}"]
    if body is not None:
        cmd += ["-d", json.dumps(body)]
    r = sh(*cmd, check=False)
    lines = r.stdout.strip().rsplit("\n", 1)
    code = int(lines[1]) if len(lines) == 2 else 0
    return code, lines[0] if lines else ""


def admin_email() -> str:
    env = {}
    env_file = ROOT / ".env"
    if env_file.exists():
        for ln in io.open(env_file, encoding="utf-8"):
            ln = ln.strip()
            if ln.startswith("ADMIN_EMAILS="):
                env["a"] = ln.split("=", 1)[1].strip()
    if not env.get("a"):
        sys.exit("[seed-dev] ADMIN_EMAILS ausente no .env")
    return env["a"].split(",")[0].strip()


def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    email = admin_email()
    print(f"[seed-dev] admin: {email}")

    # Instância dev de pé (idempotente) + bootstrap do replica set (mongo:7 com
    # --replSet precisa de rs.initiate() UMA vez; estação nova = ainda não feito).
    sh("compose", "--profile", "dev", "up", "-d", "mongo-dev")
    sh("exec", "revoa-mongo-dev", "mongosh", "--quiet", "--eval",
       "try { rs.initiate() } catch (e) { if (e.codeName !== 'AlreadyInitialized') throw e }")

    # App efêmero em Development SÓ no banco dev, sobre a imagem APP-DEV (a que
    # serve revoa.me — o seed exercita exatamente o código deployado). As overrides
    # de conexão são obrigatórias: o default dos serviços aponta para o Mongo de
    # produção.
    sh("rm", "-f", SEED_NAME)
    sh("compose", "--profile", "dev", "run", "--rm", "-d", "--name", SEED_NAME,
       "-e", "ASPNETCORE_ENVIRONMENT=Development",
       "-e", f"Mongo__ConnectionString={DEV_MONGO}",
       "-e", f"Mongo__Database={DEV_DB}",
       "app-dev")

    try:
        # Aguarda o host responder (build já feito; ~10-20s de startup).
        for _ in range(30):
            code, _ = http("GET", "/health")
            if code == 200:
                break
            time.sleep(2)
        else:
            sys.exit("[seed-dev] app efêmero não ficou healthy")
        print("[seed-dev] app efêmero (Development) healthy")

        # 1) Usuário admin (tolera re-execução: conta já cadastrada).
        code, body = http("POST", "/api/auth/register", {
            "Name": "Rodney do Carmo", "Email": email,
            "Phone": "11999998888", "BirthDate": "1985-01-01", "CouponCode": None})
        print(f"[seed-dev] register: {code}")

        # 2) dev-verify: ativa a conta (Development only) e emite JWT com role Admin.
        code, body = http("POST", "/api/auth/dev-verify", {"Email": email})
        if code != 200:
            sys.exit(f"[seed-dev] dev-verify {code}: {body[:300]}")
        token = json.loads(body)["Token"]
        print("[seed-dev] dev-verify: JWT admin emitido")

        # 3) Seed do ecossistema demo (idempotente: limpa e re-insere por chaves fixas).
        code, body = http("POST", "/api/dev/seed-catalog", {}, token)
        if code != 200:
            sys.exit(f"[seed-dev] seed-catalog {code}: {body[:300]}")
        r = json.loads(body)
        print(f"[seed-dev] seed: {r.get('Usuarios')} usuarios, {r.get('Listings')} "
              f"listings, {r.get('Comunidades')} comunidades, {r.get('Reviews')} "
              f"reviews, {r.get('CurtidasPosts')}+{r.get('CurtidasAnuncios')} curtidas, "
              f"{r.get('ComentariosAnuncios')} comentarios, {r.get('Salvos')} salvos, "
              f"{r.get('Erros')} erro(s)")
    finally:
        sh("rm", "-f", SEED_NAME)
        print("[seed-dev] container efêmero removido")


if __name__ == "__main__":
    main()
