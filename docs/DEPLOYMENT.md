# DEPLOYMENT.md — Infra local de desenvolvimento (revoa)

> Como subir e validar a **infraestrutura de backend** do revoa em ambiente local
> (Windows + Docker Desktop + PowerShell). Foco no **Marco M1**: serviços que não
> dependem de código de aplicação — MongoDB (replica set `rs0`), MinIO (bucket
> `revoa-assets`) e Postfix (MTA `mail`, e-mail transacional).
>
> **Stack central / produção:** ver `projetosia/infra/README.md` (Traefik + Cloudflared)
> e o contrato de deploy em [`../docker-compose.yml`](../docker-compose.yml).

---

## 1. Pré-requisitos

| Ferramenta | Versão validada | Observação |
|------------|-----------------|------------|
| Docker Desktop (Linux engine) | Docker 29.5.3 | Daemon deve estar rodando |
| `mongosh` | 2.8.3 | Não obrigatório: os scripts usam `docker exec` |
| PowerShell | 5.1+ | Use `;` (não `&&`); evite `$pid`/`$args` |
| Node | 24+ | Só necessário para o frontend (Fase 2) |

**Rede externa:** o MinIO (e, em produção, app/frontend) usa a rede `traefik_net`
(external). Ela costuma já existir se a infra central (`projetosia/infra`) subiu.
Se não existir, crie uma vez:

```powershell
docker network create traefik_net
```

---

## 2. Subir a infra local (PowerShell-safe)

> Subimos **apenas os serviços de imagem pronta**: `mongo`, `minio` e `mail`.
> Nomeamos os serviços explicitamente para NÃO tentar `app`/`frontend` (ainda sem
> Dockerfile — estão sob `profile:app`) nem `chain` (Fase 1).

```powershell
cd C:\Users\rodne\projetosia\revoa

# (opcional, 1x) garantir a rede externa usada pelo MinIO
docker network create traefik_net   # silencioso se já existir

# subir mongo + minio + mail (imagens prontas)
docker compose -f docker-compose.yml up -d mongo minio mail
```

> O serviço `mail` está sob `profiles: ["app"]`, mas ao **nomeá-lo explicitamente**
> na linha de comando o Docker Compose o inicia mesmo sem `--profile app`
> (e sem tentar construir `app`/`frontend`).

### 2.1 Inicializar o replica set `rs0` (MongoDB) — obrigatório, idempotente

O MongoDB sobe com `--replSet rs0`, mas o replica set precisa ser **iniciado** antes
de aceitar transações ACID (necessárias ao revoa: optimistic locking + ACID seletiva).
Rode o script (idempotente — pula se já iniciado):

```powershell
.\scripts\init-mongo-rs.ps1
```

Saída esperada (resumo):
```
[mongo-rs] Verificando/iniciando replica set 'rs0'...
  RS_RESULT initiated ok=1            # ou "already_initiated" nas execuções seguintes
[mongo-rs] Aguardando eleicao de PRIMARY ...
[mongo-rs] rs.status().ok = 1 ; myState = 1 (1 = PRIMARY)
[mongo-rs] OK - replica set pronto para transacoes ACID.
```

> ⚠️ O **healthcheck** do `mongo` no `docker-compose.yml` é defeituoso: o JS
> `try{rs.status().ok}catch(e){exit 1}` não é válido no mongosh (`exit 1` →
> SyntaxError), então o container aparece como `unhealthy` **mesmo com o rs0 saudável**.
> Valide sempre pelo script (`rs.status().ok == 1`), não pelo status do compose.
> (Correção do healthcheck fica para um ajuste futuro do compose.)

### 2.2 Criar o bucket `revoa-assets` (MinIO) — idempotente

```powershell
.\scripts\init-minio.ps1
```

Saída esperada (resumo):
```
[minio] Alias 'revoa' -> http://minio:9000 (usuario: revoa)
[minio] Garantindo bucket 'revoa-assets' ...
[minio] Buckets em 'revoa':
  [2026-08-03 12:46:41 UTC]     0B revoa-assets/
[minio] OK - bucket 'revoa-assets' presente.
```

O script usa o cliente `mc` embutido na imagem `minio/minio` (`/usr/bin/mc`).

### 2.3 Validar o SMTP em `mail:25`

O Postfix (`mwader/postfix-relay`) ouve na porta 25 e fala ESMTP. Validação feita
por uma transação SMTP real (de dentro do container, via `/dev/tcp`):

```
BANNER    -> 220 hostname ESMTP Postfix (Debian)
HELO      -> 250 hostname
MAILFROM  -> 250 2.1.0 Ok                      # aceita no-reply@revoa.me
RCPTTO    -> 250 2.1.5 Ok                      # (via IPv4 / cliente confiável)
DATA      -> 354 End data with <CR><LF>.<CR><LF>
QUEUED    -> 250 2.0.0 Ok: queued as 76E21247D50
```

> **Como o app entrega e-mail:** o módulo **Identity** (.NET + MailKit) conecta a
> `mail:25` (hostname → IPv4 da rede `internal`) e envia para destinos externos
> (usuários). O Postfix faz relay (confiança por IP, ver §5). Não há `AUTH`/`STARTTLS`
> anunciados no MTA local.

---

## 3. Estado esperado / verificação rápida

```powershell
# containers de infra
docker compose -f docker-compose.yml ps
#   revoa-minio   Up (healthy)
#   revoa-mongo   Up (unhealthy*)  *healthcheck bugado; rs0 funciona (ver §2.1)
#   revoa-mail    Up (unhealthy*)  *imagem sem `nc`; Postfix funciona (ver §2.3/§5)

# replica set
.\scripts\init-mongo-rs.ps1          # rs.status().ok = 1

# bucket
.\scripts\init-minio.ps1             # revoa-assets presente

# fila de e-mail do Postfix
docker exec revoa-mail sh -c "mailq"
```

---

## 4. Variáveis de ambiente (`.env`)

Modelo: [`.env.example`](../.env.example). Copie para `.env` e ajuste:

```powershell
Copy-Item .env.example .env
```

Variáveis **interpoladas pelo `docker-compose.yml`** (as únicas com `${...}`):

| Variável | Serviço | Default (compose) | Descrição |
|----------|---------|-------------------|-----------|
| `MINIO_ROOT_USER` | minio | `revoa` | Usuário admin do MinIO |
| `MINIO_ROOT_PASSWORD` | minio | `revoa12345` | Senha admin do MinIO (use forte fora do dev) |
| `ZENVIA_TOKEN` | app | vazio | Token Zenvia (WhatsApp/SMS OTP). Vazio = OTP por telefone desativado |

Demais configurações do app (`Mongo__*`, `Minio__*`, `Smtp__*`, `Chain__*`, etc.)
estão fixas no `docker-compose.yml`. Secrets de aplicação futuros
(JWT, Google/Apple OAuth — ADR-0013) estão listados como comentário no `.env.example`.

> Os scripts `init-*.ps1` leem `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` de `.env`
> (quando presente) para casar com as credenciais realmente usadas pelo container.

---

## 5. E-mail: DNS necessário para `revoa.me`

> Decisão: e-mail transacional auto-hospedado (MailKit + Postfix, domínio `revoa.me`).
> Ver [ADR-0014](decisions/ADR-0014-email-auto-hospedado-mailkit-postfix.md).

Para **entregabilidade real** (e-mail chegar na caixa de entrada de usuários), configure
no **Cloudflare** (gerenciador do DNS de `revoa.me`) os registros abaixo. **Localmente**
eles não são necessários — servem para o MTA assinar/validar ao entregar ao mundo.

### MX
Aponta o domínio para o servidor de e-mail (host com IP público / túnel que exponha a 25).

| Tipo | Nome | Valor | Prioridade |
|------|------|-------|------------|
| MX | `revoa.me` | `mail.revoa.me` | 10 |

> Requer um registro **A**/AAAA para `mail.revoa.me` apontando para o IP público do host
> (ou do túnel/relay). O servidor precisa de **porta 25 acessível** e **IP com reputação**.

### SPF (autoriza o MX a enviar)
```
revoa.me.   TXT   "v=spf1 mx ~all"
```

### DKIM (assinatura — OpenDKIM no container `mail`)
O container gera automaticamente a chave (selector `mail`, domínio `revoa.me`).
Publique a chave pública como TXT. Obtenha o registro exato (atual) com:

```powershell
docker exec revoa-mail sh -c "cat /etc/opendkim/keys/revoa.me/mail.txt"
```

Registro a criar (exemplo do formato gerado — **use a saída do comando acima**,
pois a chave é gerada por container):

```
mail._domainkey.revoa.me.   TXT   (
  "v=DKIM1; h=sha256; k=rsa; "
  "p=<chave-publica-rsa-base64-do-comando-acima>" )
```

> A chave DKIM **privada** fica no volume `revoa-mail-dkim` (não commitar). Se o volume
> for removido, uma nova chave é gerada e o registro DNS precisa ser republicado.

### DMARC (política progressiva)
Comece em `quarantine` e endureça para `reject` após validar:

```
_dmarc.revoa.me.   TXT   "v=DMARC1; p=quarantine; rua=mailto:postmaster@revoa.me; pct=100; adkim=s; aspf=s"
```

---

## 6. Limitação importante: entregabilidade real de e-mail

> **No ambiente local só é possível validar a aceitação na porta 25** (o Postfix
> recebe a mensagem e a enfileira — `250 Ok: queued as ...`). **Entregar de fato**
> na caixa de entrada de provedores (Gmail, Outlook, etc.) exige:

- **IP público** na porta 25 (residencial/nuvem costuma bloquear saída na 25).
- **Reputação de IP** (warmup; IPs novos vão para spam).
- **DNS completo** (MX + SPF + DKIM + DMARC, §5) apontando para o servidor real.
- **rDNS (PTR)** coerente: o IP deve resolver reverso para `mail.revoa.me`.

Para o **MVP** sem custo (decisão ADR-0014), o Postfix local atende ao fluxo de
desenvolvimento. Se a entregabilidade cair em escala, reavaliar um SaaS
(SendGrid/Mailgun/SES) como relay/smarthost — configurável no Postfix
(`relayhost`) sem mudar o app (MailKit continua entregando a `mail:25`).

---

## 7. Troubleshooting

### MongoDB — `NotYetInitialized` / `rs.status()` falha
- O replica set só funciona após `.\scripts\init-mongo-rs.ps1` (uma vez por volume novo).
- Se aparecer `alreadyInitiated`/`already a member`: normal, é idempotente.
- Reset completo (apaga dados!): `docker compose down -v` e recriar.
- **Membro anunciado como `mongo:27017`** (DNS interno do compose) — não mude para
  `localhost`, senão o app (.NET) não consegue alcançar o node.

### MinIO — credenciais / acesso negado
- O script lê `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` de `.env` (ou usa defaults).
- Se o container subiu com outras creds, atualize `.env` e reinicie o MinIO:
  `docker compose restart minio` (depois rode `init-minio.ps1`).
- Console admin: via Traefik em `assets.revoa.me` (Fase 1+) ou direto pela porta 9001
  em dev (quando exposta).

### Mail — porta 25 / relay
- **`Relay access denied` (454 4.7.1) conectando via `localhost`:** o `mynetworks`
  do Postfix é `0.0.0.0/0` (**só IPv4**); `localhost` resolve para IPv6 `::1`, que
  **não** é coberto → relay negado. **O app conecta via hostname `mail` → IPv4 → funciona.**
  Para testes manuais, conecte a `127.0.0.1` (IPv4) ou ao IP do container.
- **Nenhum relay para domínios externos sem IP confiável:** `defer_unauth_destination`
  protege contra open-relay. Clientes da rede `internal` (app) são confiáveis.
- **`nc: not found` no healthcheck do `mail`:** a imagem `mwader/postfix-relay` **não
  tem `nc`**, então o healthcheck do compose sempre falha (container aparece
  `unhealthy`) **mesmo com o Postfix saudável**. Valide pela transação SMTP (§2.3).
- Ver fila: `docker exec revoa-mail sh -c "mailq"`.
- Ver logs: `docker compose logs mail`.

### Geral — `network traefik_net declared as external, but could not be found`
- Crie: `docker network create traefik_net` (§1). Necessário porque o MinIO usa essa rede.

### Docker — `failed to connect to the docker API`
- Docker Desktop parado. Inicie-o e aguarde o daemon (Linux engine) ficar pronto:
  `docker info` deve responder sem erro.

---

## 8. Scripts de infra (`scripts/`)

| Script | O que faz | Idempotente |
|--------|-----------|-------------|
| `scripts/init-mongo-rs.ps1` | Inicia/valida o replica set `rs0` (via `docker exec revoa-mongo`) | ✅ pula se `rs.status().ok==1` |
| `scripts/init-minio.ps1` | Cria/valida o bucket `revoa-assets` (via `mc` embutido no container) | ✅ ignora se já existir |

Ambos assumem os containers já no ar (`docker compose up -d mongo minio mail`).

---

*Última atualização: Marco M1 — infra local de dev (03/08/2026).*
