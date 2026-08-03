# revoa.me — Economia circular e de ajuda mútua, sem fins lucrativos

> Plataforma de troca e doação de produtos e serviços com **moeda social comunitária (RVM)**,
> **auto-custódia** (carteira invisível) e **ajuda mútua** hiperlocal. Sem fins lucrativos.
> Sucessor/sunset do `trocadeira` (Python). Herdeira digital das moedas sociais brasileiras (Banco Palmas).

**Domínios:** `revoa.me` (app) · `revoa.org` (blog/transparência/impacto)
**Stack:** Backend único **.NET 10** · Frontend único **React + TypeScript** · Solidity/Foundry · Avalanche Subnet-EVM · MongoDB · MinIO · Traefik+Cloudflared
**Status:** ✅ **Fase 0 concluída** (documentação viva, OOUX, pesquisa, identidade visual, ADRs) → próximo: **Fase 1** (infra de chain + foundation).

---

## Comece aqui (mapa de continuidade)

> Este README é o **índice central** do projeto. Use-o para retomar o desenvolvimento.

| Quer... | Leia |
|---------|------|
| Entender o negócio | [`docs/BUSINESS.md`](docs/BUSINESS.md) |
| Regras operacionais (escrow, doação, acesso, cadastro) | [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md) |
| A economia do RVM | [`docs/TOKENOMICS.md`](docs/TOKENOMICS.md) |
| Arquitetura técnica | [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) |
| Fluxos do usuário (jornadas + Gherkin) | [`docs/USER_FLOWS.md`](docs/USER_FLOWS.md) |
| Objetos do produto (OOUX/ORCA) | [`docs/OOUX.md`](docs/OOUX.md) |
| Mercado + Lei 14.478 + benchmark cadastro | [`docs/MARKET_RESEARCH.md`](docs/MARKET_RESEARCH.md) |
| Marca, logos, slogans, aplicações | [`docs/VISUAL_IDENTITY.md`](docs/VISUAL_IDENTITY.md) |
| **Roadmap / Fases (0–4 detalhadas)** | [`docs/ROADMAP.md`](docs/ROADMAP.md) |
| Decisões (ADRs) | [`docs/decisions/README.md`](docs/decisions/README.md) |
| Memória do projeto (padrões, armadilhas, stack) | [`AGENTS.md`](AGENTS.md) |
| Deploy/containers | [`docker-compose.yml`](docker-compose.yml) + `projetosia/infra/README.md` |

---

## Decisões travadas (resumo — ver ADRs)

- **Sem fins lucrativos** · taxa 2% → **Fundo Comunitário** (sem lucro; transparente no `revoa.org`).
- **Moeda social RVM** (utility token, não ativo financeiro; não conversível em BRL; demurrage queima).
- **Doação = produto de primeira classe** (produto+serviço); recompensa multi-eixo (reputação + bônus RVM admin + pontos de ajuda).
- **Auto-custódia** (Safe + 4337 + passkey WebAuthn); gas invisível (Paymaster).
- **Cadastro:** cupom **opcional** · confirmação **e-mail (MailKit+Postfix, revoa.me) + WhatsApp/SMS (Zenvia)** · idade **18+** · CPF **opcional** · login **Google+Apple** · avatar **auto-gerado**.
- **Acesso:** aberto p/ navegar, fechado p/ agir (anônimo vê tudo; ação exige login+verificação).
- **Backend único .NET; frontend único React/TypeScript.**
- **Dois domínios** (.me app / .org transparência); natureza jurídica informal por ora (formalizar OSC pré-público).

---

## Estrutura do projeto (limpa — sem pastas vazias)

```
revoa/                           ← estado atual (limpo; pastas de código entram com conteúdo nas fases)
├─ README.md                     ← este arquivo (índice central)
├─ AGENTS.md                     ← memória do projeto (stack, padrões, armadilhas)
├─ docker-compose.yml            ← contrato de deploy (traefik_net + interna; profiles app/chain)
├─ .gitignore
├─ .kilo/skills/ooux/SKILL.md    ← skill OOUX (objects-first / ORCA)
├─ docs/                         ← TODA a documentação viva (ver mapa abaixo)
│  ├─ BUSINESS.md  TOKENOMICS.md  ARCHITECTURE.md  BUSINESS_RULES.md
│  ├─ OOUX.md  USER_FLOWS.md  MARKET_RESEARCH.md  ROADMAP.md
│  ├─ VISUAL_IDENTITY.md  VISUAL_EXPLORATIONS.md
│  ├─ decisions/                 ← ADR-0001 … ADR-0014 (+ README índice)
│  └─ visual-explorations/       ← 12 PNGs + 2 scripts de render (render_*.py)
├─ src/                          ← (Fase 1) Clean Architecture + Modular Monolith + DDD (Revoa.sln, Revoa.Api, modules/, shared/)
├─ contracts/                    ← (Fase 1, com código) Solidity + Foundry
├─ chain/                        ← (Fase 1, com código) config Avalanche Subnet-EVM
├─ frontend/                     ← (Fase 2) React + TypeScript (Vite SPA) + PWA
└─ tests/  .github/workflows/    ← (Fase 1+) xUnit + Testcontainers + CI
```

> **Princípio de limpeza:** pastas de código só existem no repo **quando têm conteúdo** (sem placeholders
> vazios). A estrutura .NET definitiva (Clean Architecture por módulo + DDD) está detalhada em
> [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) (seção "Estrutura da solution").

### Mapa de arquivos (documentação)
| Arquivo | Conteúdo |
|---------|----------|
| `docs/BUSINESS.md` | Proposta de valor, pivot, 4 modos, doação, personas, moat |
| `docs/BUSINESS_RULES.md` | Acesso (anônimo×auth), anúncio, escrow, doação, comunidades, cadastro, reputação |
| `docs/TOKENOMICS.md` | RVM (moeda social), emissão, demurrage IPCA, taxa→Fundo, cupom opcional, comparativo |
| `docs/ARCHITECTURE.md` | Monólito modular, MongoDB, AA, Indexer, contratos, autorização, containers |
| `docs/USER_FLOWS.md` | F0–F12: jornadas + estados + Gherkin (navegação, registro, anúncios, carteira, doação, troca, comunidade) |
| `docs/OOUX.md` | Mapa ORCA de **26 objetos + 32 fluxos (UF-01..UF-32)** → aggregates DDD (+ matriz de relacionamentos) |
| `docs/ROADMAP.md` | **5 fases detalhadas** (0–4): entregáveis, fluxos cobertos, DoD, dependências, riscos |
| `docs/MARKET_RESEARCH.md` | C2C + economia solidária + moedas sociais + plataformas sem FLP + Lei 14.478 + benchmark cadastro |
| `docs/VISUAL_IDENTITY.md` | Marca, 5 elementos (logo/slogan/ícone/favicon/RM$), slogans, aplicações web/mobile |
| `docs/VISUAL_EXPLORATIONS.md` | 6 logos comunidade + 6 Web3/híbridas (arquivo) |
| `docs/decisions/README.md` | Índice dos 14 ADRs |
| `docs/decisions/ADR-0001…0014.md` | Decisões de arquitetura/negócio (chain, AA, monólito, utility token, mint, React, MinIO, OOUX, sem FLP, doação, domínios, .NET, cadastro, e-mail) |

### ADRs (decisões)
| ADR | Decisão |
|-----|---------|
| 0001 | Avalanche Subnet-EVM própria (privada→pública, mesma chain) |
| 0002 | AA: Safe + 4337 + webauthn-solidity (carteira invisível) |
| 0003 | Monólito modular + MongoDB isolado por coleção |
| 0004 | RVM = utility token (Lei 14.478); jurídico informal por ora |
| 0005 | Mint-to-escrow (produto) + mint-on-purchase (serviço) |
| 0006 | Frontend React/TypeScript (Vite SPA) |
| 0007 | MinIO para storage no MVP (IPFS só no público) |
| 0008 | OOUX objects-first (ORCA) |
| 0009 | Sem fins lucrativos — taxa 2% → Fundo Comunitário |
| 0010 | Doação/Voluntariado com recompensa multi-eixo (bônus RVM admin) |
| 0011 | Dois domínios: revoa.me (app) + revoa.org (transparência) |
| 0012 | Backend único .NET; frontend único React/TypeScript |
| 0013 | Cadastro: campos, verificação dupla (e-mail+WhatsApp/Zenvia), Google+Apple |
| 0014 | E-mail auto-hospedado (MailKit + Postfix, domínio revoa.me) |

---

## Como rodar (infra-base já funcional)

```powershell
# 1) Infra central (uma vez por boot)
cd C:\Users\rodne\projetosia\infra
docker network create traefik_net   # só na 1ª vez
docker compose up -d                # Traefik + cloudflared

# 2) revoa — infra-base (mongo + minio) já roda hoje
cd C:\Users\rodne\projetosia\revoa
docker compose up -d                # serviços sem profile

# 3) Fase 1+ (com Dockerfiles): app + mail
docker compose --profile app up -d --build
# Fase 1: chain
docker compose --profile chain up -d
```

> Roteamento `revoa.me`: file-provider `projetosia/infra/traefik/dynamic/routers-revoa.yml` → `revoa-app:8000`.
> No cutover (Fase 4), o container Python do trocadeira é removido e o `revoa-app` (.NET) assume — sem mudança de DNS.

---

## Próximos passos — Fase 1 (infra de chain + foundation)

Conforme o plano-fonte-de-verdade (`~/.local/share/kilo/plans/1785722983643-revoa-defi-platform.md` §11):
1. `contracts/` Foundry: `RVM`, EntryPoint canônico, Safe+4337+recovery+Coinbase webauthn-solidity, Paymaster, `CouponRedeemer`. `forge test`.
2. Bundler Stackup + Paymaster (ativar `profile:chain`).
3. `Revoa.sln` + `shared/` + `Revoa.Api` (host único + SignalR + `/health`) + isolamento por módulo (teste que falha em query cross-coleção).
4. **Identity** (cupom opcional, verificação e-mail+WhatsApp, login Google+Apple, MailKit+Postfix), **Account** (Safe, saldo), **Indexer** (idempotente), **Token** (faucet R$20→mint; cupom on-chain).

> Em toda feature nova: partir do mapa [`docs/OOUX.md`](docs/OOUX.md) (skill `ooux`) e documentar regras em [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md). Commitar ao final de cada tarefa.

---

*Última atualização: Fase 0 (03/08/2026) — rodada v3.1 (cadastro + email + acesso).*
