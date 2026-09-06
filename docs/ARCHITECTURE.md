# ARCHITECTURE.md — revoa.me

> **Monólito modular** (on-chain + off-chain), tokenizado, self-custody. Princípios: SOLID + Clean
> Code + DDD + Clean Architecture por módulo; aggregates alinhados aos objetos OOUX; YAGNI estrito;
> isolamento de coleções MongoDB; estado financeiro autoritativo **on-chain**.

Referências: Plano-fonte-de-verdade §2–§7 · blueprint `equivale/dev/AGENTS.md` · `BUSINESS.md` · `TOKENOMICS.md`.

---

## 1. Visão Geral

```
                         ┌──────────────────────────────────────┐
                         │        Cloudflare + Traefik          │  (infra rodne/infra)
                         │   revoa.me → revoa-app:8000          │
                         └───────────────────┬──────────────────┘
                                             │ HTTPS / X-Forwarded-*
                         ┌───────────────────▼──────────────────┐
                         │   Revoa.Api (ASP.NET 10, host único) │
                         │   endpoints + JWT + SignalR hub      │
                         │   /health                            │
                         └──┬────────────────────────────────┬──┘
            eventos in-process (MediatR)        │   txs assinadas (Nethereum/RPC)
        ┌─────────────────────────────────────┐ │
        ▼  13 módulos (bounded contexts)       ▼ ▼
  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐        ┌──────────────────┐
  │Identity  │ │Account   │ │Catalog   │ │Exchange  │ …      │ BlockchainIndexer│
  │(JWT/Auth)│ │(Safe 4337)│ │(Listings)│ │(Escrow)  │        │ (source of truth │
  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘        │  on-chain → read │
       │            │            │            │              │  models idempot.)│
       └────────────┴────────────┴────────────┴──── MongoDB ─┤ (projeções)      │
                              (replica set rs0)              └────────┬─────────┘
                                                                    │ eventos
                         ┌──────────────────────────────────────────▼─────┐
                         │  Avalanche Subnet-EVM própria (rede interna)   │
                         │  RVM(ERC-20) · ProductNFT(721) · ServiceVoucher(1155)
                         │  EscrowVault · Treasury · CouponRedeemer        │
                         │  Safe + módulo4337 + webauthn-solidity + Paymaster
                         └────────────────────────────────────────────────┘
   MinIO (metadata/imagens NFT) · Ollama Qwen 7B (preços) · bundler Stackup · paymaster
```

> **Situação atual vs roadmap:** o Indexer dedicado e a integração bundler/paymaster (AA) são
> **roadmap** — hoje o backend assina com EOAs managed (ADR-0015) e consulta a chain diretamente
> via Nethereum. Os containers `bundler`/`paymaster` já existem no compose (`profile: chain`)
> como contrato de infra prontos para uso. A integração MinIO (upload de imagens/assets) também
> é **roadmap**: o container existe no compose, mas nenhum código .NET o usa ainda.

### Containers (compose revoa — segue o protocolo da infra central `rodne/infra`)
| Container | Rede | Perfil | Observação |
|-----------|------|--------|------------|
| `revoa-app` (.NET) | internal + traefik_net | app | host único; `revoa.me` via file-provider |
| `revoa-frontend` (Vite dev) | internal + traefik_net | app | prod: SPA servida pelo app |
| `revoa-mongo` (rs0) | **internal** | — | replica set; ACID seletiva |
| `revoa-minio` (S3) | internal + traefik_net | — | `assets.revoa.me`; tokenURI |
| `revoa-avalanche` | **internal** | chain | nó Subnet-EVM |
| `revoa-bundler` (Stackup) | **internal** | chain | ERC-4337 |
| `revoa-paymaster` | **internal** | chain | gas invisível |
| `revoa-ollama` (Qwen 7B GPU) | **internal** | chain | normalização de preços |
| `revoa-mail` (Postfix+OpenDKIM) | **internal** | app | MTA leve auto-hospedado (e-mail transacional, domínio revoa.me); MailKit (.NET) envia; ver ADR-0014 |

---

## 2. Princípios Arquiteturais

1. **SOLID + Clean Code + DDD + Clean Architecture** por módulo; aggregates = objetos OOUX.
2. **YAGNI estrito** — gate de review + CI detecta símbolos sem consumidor.
3. **Isolamento de módulos** — coleções MongoDB **próprias**; **proibido** query cross-coleção; comunicação só via `IIntegrationEventBus` (in-process → MassTransit ao extrair). Teste falha ao detectar query cross-coleção.
4. **CQRS + MediatR**; domínio puro (sem dependência de infra).
5. **MongoDB** — optimistic locking (`Version`); **ACID seletiva só na finalização financeira**; tratar legados.
6. **Versionar tudo** (contratos, eventos, migrations de schema).
7. pt-BR doc/UI; inglês no código.

---

## 3. Bounded Contexts (13 módulos — `src/modules/`)

| Módulo | Responsabilidade | Coleções |
|--------|------------------|----------|
| **Identity** | Registro (cupom **opcional** + verificação **e-mail e telefone**), JWT passwordless, roles (User/Mod/Admin/Arbitrator — ADR-0018), recuperação. **E-mail via MailKit→Postfix** (ADR-0014) | `Users` |
| **Account (Wallet)** | Carteiras EOA por usuário (managed — desvio ADR-0015), saldo, transfer P2P *(roadmap — sem endpoint)* | `Accounts` |
| **Catalog** | Anúncios (`kind`+VOs), categorias (seed canônico `CategorySeed`), **modo**, **visibilidade**, comentários recursivos, busca, comparativo, **feed por geolocalização** (filtros `kind`/categoria/comunidade/modo/preço/`sellerIds`) | `Listings`, `Categories`, `Comments` |
| **Exchange** | Máquina de estados escrow (purchase/redeem/release/dispute/cancel/resolve) + fila de doação/voluntariado | `Trades`, `HelpRequests` |
| **Token (Treasury / Fundo Comunitário)** | Faucet R$20, mint/burn RVM, taxa 2%→Fundo Comunitário (sem FLP) | (on-chain; sem coleção) |
| **Coupon** | Cupom on-chain: criação admin, resgate (mint RVM), revogação | `Coupons` |
| **Demurrage** | Demurrage IPCA-trimestral (preview/run sob demanda via admin — sem job agendado; queima) | `DemurrageRuns` |
| **Community** | Default + user-created; criador+moderadores+membros; posts recursivos (materialized path depth 6); chat SignalR | `Communities`, `Memberships`, `Posts`, `ChatMessages` |
| **Pricing** | Referência de preço justo por categoria (mediana comunitária + BRL seed + IPCA/IBGE + Ollama) | `PriceReferences` |
| **Moderation** | Denúncias + resolução admin (ban via evento → Identity) | `Reports` |
| **Notifications** | In-app + Web Push | `Notifications`, `PushSubscriptions` |
| **Reputation** | Avaliações 1–5 pós-troca, agregados por usuário | `Reviews`, `Reputations` |
| **Admin** | Parâmetros runtime tipados compartilhados entre módulos | `SystemParameters` |

> **Host:** API ASP.NET única (endpoints + auth + hubs SignalR `/hubs/community` e `/hubs/notifications`; JWT via query string `access_token`). Sem YARP. Reverse proxy: Traefik + Cloudflared.

### Estrutura da solution — Clean Architecture + Modular Monolith + DDD
> Padrão: **Clean Architecture/Onion** (ApplicationCore no centro, sem deps de infra) + **modular monolith**
> (cada bounded context = módulo) + **DDD** (aggregates por pasta + SeedWork). Refs: Microsoft
> *Architect Modern Web Apps* (eShopOnWeb / [ardalis/cleanarchitecture](https://github.com/ardalis/cleanarchitecture));
> eShopOnContainers (DDD/CQRS). As pastas são criadas **com conteúdo** na Fase 1 (não há pastas vazias no repo).

```
revoa/
├─ src/
│  ├─ Revoa.sln
│  ├─ Revoa.Api/                        ← UI/composition root (host único)
│  │   ├─ Controllers/  Filters/  Middleware/  Hubs/(SignalR)  Program.cs
│  │   └─ /health, JWT, DI wiring (referencia módulos + Infrastructure; SEM lógica de negócio)
│  ├─ modules/                          ← 13 bounded contexts (monólito modular)
│  │   └─ <context>/                    ← identity, account, catalog, exchange, token, coupon,
│  │       ├─ Domain/                      demurrage, community, pricing, moderation,
│  │       │   ├─ Aggregates/<Aggregate>/   notifications, reputation, admin
│  │       │   │   ├─ <Entity>.cs  <ValueObject>.cs  (aggregate root + VOs)
│  │       │   │   └─ Events/  Exceptions/  Specifications/
│  │       │   ├─ Repositories/ (interfaces — contratos de persistência)  IUnitOfWork
│  │       │   └─ SeedWork/ (Entity, ValueObject, AggregateRoot base — sem redundância)
│  │       ├─ Application/              ← CQRS (MediatR): Commands/Queries/Handlers/DTOs/Validators(FluentValidation)
│  │       └─ Infrastructure/           ← MongoDB (coleção PRÓPRIA do módulo), serviços (MailKit/Nethereum), integrações
│  └─ shared/
│     ├─ Abstractions/                  ← IIntegrationEventBus, Result, ApiError (contrato HTTP), guards
│     ├─ IntegrationContracts/          ← eventos + DTOs de integração (versionados) entre módulos
│     ├─ Application/                   ← kernel CQRS: ValidationBehavior + AddRevoaCQRS (ADR-0016)
│     └─ Infrastructure/                ← MongoRepositoryBase (optimistic locking), IMongoIndexEnsurer
├─ contracts/                           ← Solidity + Foundry: RVM, ProductNFT, ServiceVoucher,
│                                          EscrowVault, Treasury, CouponRedeemer (forge test)
├─ frontend/                            ← React + TypeScript (Vite SPA) + PWA
├─ src/test/                            ← Revoa.IntegrationTests (OffChain no CI; OnChain com anvil)
└─ .github/workflows/                   ← CI: dotnet build/test + forge build/test + npm build/typecheck
```

**Regras de dependência (Clean Architecture):**
- `Domain` → **zero** dependências (nem Infrastructure, nem Application). É o centro.
- `Application` → depende só de `Domain` + `shared/Abstractions`.
- `Infrastructure` → implementa interfaces de `Domain`/`Application`; referencia pacotes (MongoDB driver, Nethereum, MailKit, Zenvia).
- `Revoa.Api` → composition root: referencia módulos + `Infrastructure` (somente p/ wiring DI); **SEM lógica de negócio**.
- **Isolamento de módulos:** um módulo **não referencia** o Infrastructure/Domain de outro. Comunicação só por **ID** (referência) + **eventos** (`shared/IntegrationContracts` via `IIntegrationEventBus`, in-process MediatR). Teste deve **falhar** ao detectar query cross-coleção.

**Tipos por camada (padrão Microsoft/DDD):**
- *Domain:* Entities, Aggregates, Value Objects, Domain Services, Specifications, Domain Events, Exceptions, SeedWork.
- *Application:* Commands/Queries (CQRS), Handlers, DTOs, Validators, Mappers, Integration Event handlers.
- *Infrastructure:* Repositories (MongoDB), UnitOfWork, serviços externos (chain via Nethereum, e-mail MailKit, WhatsApp Zenvia, ViaCEP, Ollama; MinIO é roadmap).

---

## 4. Persistência — MongoDB (blueprint `equivale`)

### Convenções
- **PascalCase** na serialização (`"Version"`, `"SellerId"`) — sem camelCase automático. Exceção: `_id`.
- **Optimistic locking:** toda entidade tem `public long Version { get; set; }`.
  `UpdateAsync` filtra `_id + Version == expected`; `MatchedCount == 0` → `ConcurrencyException`.
- **Docs legados** (sem `Version`): filtro com `$or: [{Version: expected}, {Version: {$exists:false}}]`.

### ACID seletiva
- `IUnitOfWork.ExecuteInTransactionAsync` envolve **APENAS** a finalização financeira
  (`FinishTransactionAsync`): libera/queima RVM, transfere NFT/voucher, cobra taxa, debita estoque.
- Create/Cancel usam **optimistic locking + escrow** (sem transação ACID).
- Motivo: equilíbrio entre consistência (no ponto crítico) e performance/throughput do dia a dia.

### Anti-N+1 (embed de nomes)
- Listings embedam `SellerName`/`SellerAvatarUrl`/`CommunityName` (populados no command handler).
- `DtoEnricher` = fallback para legados sem os campos.
- Moderadores/comunidade embedam nomes relevantes para leitura sem join.

### Replica set
- `rs0` (dev local ou Docker). Necessário para transações ACID.

---

## 5. On-chain — Smart Contracts (Solidity + Foundry)

### Contratos (`contracts/`)
| Contrato | Padrão | Responsabilidade |
|----------|--------|------------------|
| **RVM** | ERC-20 | Moeda de troca; roles `MINTER`/`BURNER`; faucet/cupom/demurrage consomem `burn` |
| **ProductNFT** | ERC-721 | Produto tokenizado; `mintToEscrow()` ao listar (NFT nasce no Vault) |
| **ServiceVoucher** | ERC-1155 | Voucher de serviço; mint-on-purchase; `redeem`/`confirm`; validade 30d |
| **EscrowVault** | atomic swap + role `ARBITRATOR` | Custódia NFT/RVM; janela 72h `block.timestamp`; claim pós-expira; 2%→Fundo Comunitário (contrato `Treasury`) |
| **Treasury** (Fundo Comunitário) | — | Recebe taxa 2% (**sem fins lucrativos**, reinvestido na operação); role `MINTER`/`ARBITRATOR` da plataforma |
| **CouponRedeemer** | on-chain | Valida cupom (`maxUses`/`expiry`) → mint RVM; unificado com convite |
| **Safe + módulo 4337 + recovery(admin+timelock)** | AA | Custódia do usuário; EntryPoint canônico |
| **Coinbase webauthn-solidity** | P-256 | Signer WebAuthn (passkey) dentro de cada Safe |
| **Paymaster (verifying)** | ERC-4337 | Patrocina gas (usuário não vê ETH) |

### Máquinas de estado
> Estados reais do aggregate `Trade` (`TradeState`): **Offered · Funded · Released · Disputed · Refunded · Cancelled**
> (espelham o `EscrowVault` on-chain). Não há estado "entregue": a logística é combinada fora da
> plataforma e a liberação (`release`) é cooperativa (vendedor **ou** comprador) a qualquer momento
> após `Funded`, ou automática após 72h sem disputa. No serviço, o `redeem` do voucher é um flag
> (`VoucherRedeemed`) — o estado permanece `Funded`.

```
PRODUTO:
  Listar(mint NFT→Vault) → Offered → Funded(buyer Block)
        → Released(RVM −2% → seller, NFT → buyer; cooperativa ou auto após 72h) | Disputed → árbitro
  Cancelled → Refunded(RVM→buyer, NFT→seller)

SERVIÇO:
  Offered → Funded(RVM block + voucher→buyer) → Redeem(flag)
        → [72h] → Released(RVM −2% → provider, voucher burn) | Disputado → árbitro
  Expiry(30d) → Refunded(RVM→buyer)
```

### Consistência on/off-chain
- **Estado financeiro autoritativo ON-CHAIN.** Hoje o backend consulta a chain diretamente (Nethereum); Indexer com read models idempotentes é roadmap.
- **Ações do usuário = assinadas pelo backend com a EOA managed** (ADR-0015); **admin = role `ARBITRATOR`** (ADR-0018).
- **Reorgs:** aguardar N confirmações antes de considerar final.
- **Doação/voluntariado:** reusam o EscrowVault/voucher com **valor 0** (mesma atomicidade; sem RVM do receptor). Recompensa multi-eixo (reputação + bônus RVM admin + pontos) creditada off-chain ao doador/voluntário.

### Ferramental Foundry
- `forge test` (unit, todos os estados) · `forge coverage` (≥90% em EscrowVault/RVM/NFT/Voucher/Paymaster/CouponRedeemer) · `anvil` (nó local p/ Testcontainers).

---

## 6. Account Abstraction (carteira invisível) — **roadmap** (ADR-0002)

> **Hoje (ADR-0015):** carteiras **EOA plaintext managed** pelo backend — o usuário tem a mesma
> UX (carteira invisível), mas quem assina é o backend via Nethereum. Sair disso (AA completa ou
> KMS) é **blocker pré-público**. Alvo: **Safe + módulo 4337** + **EntryPoint canônico** +
> **bundler Stackup** + **verifying paymaster** + **Coinbase `webauthn-solidity`** (signer P-256).

### Onboarding UX alvo (carteira invisível)
1. Usuário cria conta com **email + passkey (WebAuthn)** — **sem seed phrase**.
2. Backend (via bundler) cria uma **Safe** com módulo 4337 + signer `webauthn-solidity`.
3. **Faucet R$20** minta RVM na nova Safe.
4. Recuperação: **admin + timelock** (privado). Futuro público: **guardians (M-de-N)**.
5. **Gas invisível**: o Paymaster patrocina; o usuário só vê RVM.

### Interação com a chain
- Alvo: toda ação on-chain do usuário é uma **UserOperation** (assinada pela Safe, enviada ao bundler Stackup).
- O Paymaster assina off-chain (verifying); o contrato Paymaster verifica on-chain.
- **Indexer** (roadmap) escutaria eventos do EntryPoint + contratos → atualizaria saldos/estados off-chain.

---

## 7. BlockchainIndexer (source of truth) — **roadmap**

- Consome eventos on-chain (`Transfer`, `Escrow*`, `Mint`, `Burn`, `Redeem`, `CouponRedeemed`...).
- Projeção **idempotente**: chave composta `txHash + logIndex` (nunca aplica o mesmo evento 2×).
- Read models em `Accounts` (saldos) e `Trades` (estado do escrow) — **consultados pelo app**; o saldo on-chain é a verdade, o read model é o cache de leitura.
- Restart-safe: rastreia último bloco processado; reprocessa em caso de gap.
- Confirmações: eventos só "finalizam" o fluxo de UI após N confirmações.

---

## 7b. Autorização (anônimo × autenticado)
- **Aberto para navegar, fechado para agir.** Endpoints `GET` públicos (feed, listing, comunidade, perfil,
  posts, busca) liberados a anônimos.
- **Gate de ação:** todo endpoint que **modifica estado** (ofertar, pedir/doar, publicar, postar, chat,
  transferir, avaliar) exige `[Authorize]` + verificação dupla (claim `email_verified` + `phone_verified`).
- CTA anônimo → cadastro (F1) com retorno ao contexto. Detalhes: `BUSINESS_RULES.md` (Acesso) · `USER_FLOWS.md` (F0).

## 8. Frontend (React + TypeScript SPA + PWA)

> Stack: **React + TypeScript (Vite SPA) + Tailwind + SignalR client + PWA.** (Único backend: .NET; único framework frontend: React/TypeScript. `viem`/`permissionless.js`/Safe SDK entram junto com a AA — roadmap.)

- **Carteira invisível:** hoje managed pelo backend (ADR-0015); alvo: passkey → cria Safe (via Safe SDK + permissionless.js; viem p/ assinar).
- **Feed dinâmico por `kind`:** um objeto Anúncio; o frontend renderiza campos conforme `kind` (ProductDetails vs ServiceDetails).
- **Troca tracker:** acompanha estado do escrow em tempo real (SignalR; Indexer é roadmap).
- **Comunidade:** posts recursivos (materialized path, depth 6) + chat SignalR.
- **PWA instalável** (manifest + service worker). Next.js/SSR só p/ marketing/SEO depois.
- Em produção, a **SPA é servida pelo app .NET** (host único); o container `frontend` é dev-only.

---

## 9. Anúncio (OOUX — 1 objeto, `kind`)

> Princípio OOUX: o usuário pensa em **"uma coisa que oferece"**, não em "produto OU serviço". Logo, **um** objeto `Anúncio` com `kind` e **value objects tipados**.

```
Anúncio (Listing)
├─ id, title, description, images, category, seller(embedded), location(lat/lng, bairro, cidade)
├─ kind: "product" | "service"
├─ mode: "trocar" | "repassar" | "doar" | "voluntariar"
├─ visibility: "comunidade" | "global" | "ambos"
├─ priceRvm + comparativo (BRL ref, mediana, sugestão)
├─ communityId (se visível a uma comunidade)
└─ details (polimórfico por kind):
     ProductDetails: condition, stock/units, nftTokenId (ao listar)
     ServiceDetails: unitType (per-service | hours), duration, voucherExpiry
```

O frontend renderiza o formulário/detalhe **dinamicamente** conforme `kind` + `mode`.

---

## 10. Comunidades & Tempo Real (SignalR)

- **Comunidades:** default (plataforma/cidade) + user-created (criador=admin).
  Geográficas (condomínio/bairro/cidade/região) + geolocalização (ViaCEP + HTML5).
- **Hierarquia:** criador → moderadores → membros. Moderadores agem em anúncios+posts da própria comunidade.
- **Posts recursivos:** **materialized path**, profundidade **6** (resposta a resposta até 6 níveis). Moderação em cascata (ocultar propaga a descendentes).
- **Chat geral:** SignalR, retenção **90 dias**, moderado como posts.
- **Notificações:** SignalR (in-app) + Web Push (PWA).

---

## 11. PricingIntelligence (Fase 3)

- **Recálculo sob demanda:** endpoint admin (`POST /api/pricing/refresh`): API ML + seed admin + comunidade → normalização **Ollama Qwen 7B (GPU)** → referência BRL por categoria.
- **Cotação BRL de referência:** `GET /api/pricing/rate` (público; conversão RVM↔BRL para exibição).
- **Mediana RVM** dos listings + **sugestão justa** (faixa RVM).
- **webfetcher trimestral:** IPCA/IBGE → reajusta parâmetros (faucet/cupom + base demurrage).
- **Transparência:** página pública de preços/parâmetros (`/transparency` no frontend).

---

## 12. Deploy & Cutover

- **Stack central** `rodne/infra` (local `C:\Users\rodne\infra`): PaaS genérico env-driven (Traefik v3 + cloudflared + Portal; routers gerados do `.env` via `scripts/render.py`). Protocolo em `infra/README.md`.
- **Dois domínios:** `revoa.me` = **app/plataforma** (este host); `revoa.org` = **blog/docs/painel de transparência/impacto** (site institucional sem fins lucrativos, servido como conteúdo estático/blog — pode ser o mesmo app com rota dedicada ou site separado na Fase 4).
- **revoa.me** hoje roteia (routers Traefik da infra central) para `revoa-app:8000` (atualmente o Python trocadeira).
- **Cutover (Fase 4):** parar/remover container trocadeira → o `.NET revoa-app` (mesmo nome + porta) assume `revoa.me`/`www.revoa.me`. `revoa.org`/`www.revoa.org` aponta para o app (rota `/transparencia`/blog) ou site institucional. **Sem mudança de DNS** (Cloudflare→túnel→Traefik→labels).
- **Regras (NUNCA violar):** sem portas no host; sem docker socket; DB/chain só na rede `internal`; públicos na `traefik_net`; `/health` obrigatório; confiar em `X-Forwarded-*`; app em `0.0.0.0`.

---

## 13. Observabilidade & Testes

- **OpenTelemetry + Serilog.** Tracing distribuído (API → chain).
- **Testes:** xUnit + FluentAssertions + Testcontainers (Mongo + anvil) por handler/módulo; isolamento de coleção enforced; optimistic locking (Concurrent→ConcurrencyException). Foundry (`forge test`/`coverage`). Playwright (e2e). `workers:1` p/ e2e autenticados.
- **CI verde (`.github/workflows/ci.yml`, 5 jobs):** `backend` (dotnet build + testes domínio/off-chain) · `frontend` (npm build/typecheck) · `contracts` (forge build/test) · `onchain` (testes .NET↔anvil com deploy determinístico) · `e2e` (Playwright smoke com backend real + Mongo).

---

*Referência técnica-fonte-de-verdade. Sucessor da modelagem do `equivale`. Para regras operacionais: `BUSINESS_RULES.md`. Para decisões: `docs/decisions/`.*
