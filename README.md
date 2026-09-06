# revoa.me — Economia circular e de ajuda mútua, sem fins lucrativos

> Plataforma de troca e doação de produtos e serviços com **moeda social comunitária (RVM)**,
> **auto-custódia** (carteira invisível) e **ajuda mútua** hiperlocal. Sem fins lucrativos.
> Sucessor/sunset do `trocadeira` (Python). Herdeira digital das moedas sociais brasileiras (Banco Palmas).

**Domínios:** `revoa.me` (app) · `revoa.org` (blog/transparência/impacto) · `dev.revoa.org` (deploy público de desenvolvimento)
**Stack:** Backend único **.NET 10** · Frontend único **React + TypeScript** · Solidity/Foundry · Subnet-EVM · MongoDB · MinIO · Traefik+Cloudflared
**Status:** ✅ **Fases 0–3 do roadmap entregues** — monólito modular com **13 módulos**, 6 contratos Solidity (71 testes Foundry), SPA React/PWA completa, CI com 5 jobs (backend · frontend · contracts · onchain · e2e), integração on-chain ponta a ponta e deploy público em `dev.revoa.org`. Em curso: **refactoração de coerência** (segurança, shared kernel, contrato PascalCase+ApiError, docs re-sincronizadas — ADRs 0015–0018).

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
| Deploy/containers | [`docker-compose.yml`](docker-compose.yml) + infra central `C:\Users\rodne\infra\README.md` |

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
revoa/
├─ README.md                     ← este arquivo (índice central)
├─ AGENTS.md                     ← memória do projeto (stack real, padrões, armadilhas)
├─ docker-compose.yml            ← contrato de deploy (traefik_net + interna; profiles app/chain)
├─ src/                          ← backend .NET 10 (Revoa.sln)
│  ├─ Revoa.Api/                 ← host único: controllers, hubs SignalR, Program.cs
│  ├─ modules/                   ← 13 bounded contexts (Domain/Application/Infrastructure cada)
│  ├─ shared/                    ← Abstractions · Application (CQRS kernel) · Infrastructure (MongoRepositoryBase)
│  └─ test/                      ← Revoa.Domain.Tests + Revoa.IntegrationTests (OffChain + OnChain no CI; OnChain com anvil local)
├─ contracts/                    ← Solidity + Foundry (RVM, EscrowVault, ProductNFT, ServiceVoucher, CouponRedeemer, Treasury)
├─ frontend/                     ← React + TypeScript (Vite SPA) + PWA
├─ scripts/                      ← utilitários de operação (ex.: migração de campos Mongo)
├─ docs/                         ← TODA a documentação viva (ver mapa abaixo)
│  ├─ BUSINESS.md  TOKENOMICS.md  ARCHITECTURE.md  BUSINESS_RULES.md
│  ├─ OOUX.md  USER_FLOWS.md  MARKET_RESEARCH.md  ROADMAP.md
│  ├─ VISUAL_IDENTITY.md  VISUAL_EXPLORATIONS.md
│  ├─ decisions/                 ← ADR-0001 … ADR-0018 (+ README índice)
│  └─ visual-explorations/
└─ .github/workflows/            ← CI (ci.yml, 5 jobs): backend (dotnet build/test OffChain+domínio) · frontend (npm build/typecheck) · contracts (forge build/test) · onchain (.NET↔anvil) · e2e (Playwright smoke)
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
| `docs/decisions/README.md` | Índice dos 18 ADRs |
| `docs/decisions/ADR-0001…0018.md` | Decisões de arquitetura/negócio (chain, AA, monólito, utility token, mint, React, MinIO, OOUX, sem FLP, doação, domínios, .NET, cadastro, e-mail, EOA-desvio, shared kernel, contrato ApiError, role ARBITRATOR) |

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
| 0015 | Carteiras EOA plaintext — desvio temporário da AA (blocker pré-público) |
| 0016 | Shared kernel de CQRS e persistência (duplicação sistêmica eliminada) |
| 0017 | Contrato HTTP PascalCase + envelope único `ApiError` + identificadores EN |
| 0018 | Role ARBITRATOR para resolução de disputas de escrow |

---

## Como rodar (infra-base já funcional)

```powershell
# 1) Infra central (uma vez por boot)
cd C:\Users\rodne\infra
docker network create traefik_net   # só na 1ª vez
docker compose up -d                # Traefik + Portal (+ túneis: docker-compose.tunnels.yml)

# 2) revoa — infra-base (mongo + minio)
cd C:\Users\rodne\projetosia\revoa
docker compose up -d                # serviços sem profile

# 3) app + mail + chain
docker compose --profile app up -d --build
docker compose --profile chain up -d
```

> Roteamento `revoa.me`: routers Traefik gerados do `.env` da infra central (`rodne/infra`) → `revoa-app:8000`.
> No cutover (Fase 4), o container Python do trocadeira é removido e o `revoa-app` (.NET) assume — sem mudança de DNS.

---

## Próximos passos

1. **Refactoração de coerência (em curso):** cobertura de testes (domínio + endpoints sem teste + CI OnChain/Playwright) e limpeza final (duplicatas FE, mojibake, DevController).
2. **Pré-público:** sair do desvio EOA plaintext (ADR-0015) — AA/Safe (ADR-0002) ou no mínimo KMS; rotação de chaves de dev.
3. **Cutover `revoa.me`:** trocadeira (Python) → revoa-app (.NET) sem mudança de DNS (ver AGENTS.md).
4. **Indexer dedicado** (roadmap): projeção idempotente de eventos on-chain.

> Em toda feature nova: partir do mapa [`docs/OOUX.md`](docs/OOUX.md) (skill `ooux`) e documentar regras em [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md). Commitar ao final de cada tarefa.

---

*Última atualização: 06/09/2026 — refactoração de coerência (docs re-sincronizadas com o código; ADRs 0015–0018).*
