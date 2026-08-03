# ARCHITECTURE.md — revoa.me

> **Monólito modular** (on-chain + off-chain), tokenizado, self-custody. Princípios: SOLID + Clean
> Code + DDD + Clean Architecture por módulo; aggregates alinhados aos objetos OOUX; YAGNI estrito;
> isolamento de coleções MongoDB; estado financeiro autoritativo **on-chain**.

Referências: Plano-fonte-de-verdade §2–§7 · blueprint `equivale/dev/AGENTS.md` · `BUSINESS.md` · `TOKENOMICS.md`.

---

## 1. Visão Geral

```
                         ┌──────────────────────────────────────┐
                         │        Cloudflare + Traefik          │  (infra projetosia/infra)
                         │   revoa.me → revoa-app:8000          │
                         └───────────────────┬──────────────────┘
                                             │ HTTPS / X-Forwarded-*
                         ┌───────────────────▼──────────────────┐
                         │   Revoa.Api (ASP.NET 10, host único) │
                         │   endpoints + JWT + SignalR hub      │
                         │   /health                            │
                         └──┬────────────────────────────────┬──┘
            eventos in-process (MediatR)        │   user-ops (Safe) / RPC
        ┌─────────────────────────────────────┐ │
        ▼  12 módulos (bounded contexts)       ▼ ▼
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

### Containers (compose revoa — segue template `projetosia/infra`)
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

## 3. Bounded Contexts (12 módulos)

| Módulo | Responsabilidade | Coleções |
|--------|------------------|----------|
| **Identity** | Registro invite/cupom-gated, login WebAuthn/passkeys, JWT, roles, recuperação admin+timelock | `Users` |
| **Account (Wallet)** | Smart accounts (Safe) 4337, saldo (read model), transfer P2P, Paymaster/Bundler | `Accounts` |
| **Catalog** | Anúncios (`kind`+VOs), categorias, **modo**, **visibilidade**, mint on-chain, metadata (MinIO), busca, comparativo, **feed por geolocalização** | `Listings`, `Categories` |
| **Exchange** | 3 fluxos + máquina de estados escrow (atomic swap) + disputa | `Trades` |
| **Token (Treasury / Fundo Comunitário)** | RVM mint/burn, faucet R$20, demurrage (IPCA), taxa 2%→Fundo Comunitário (sem FLP), cupom on-chain, **bônus de doação (admin-configurável)** | ledger mirror |
| **Community** | Default + user-created; criador+moderadores+membros; posts recursivos (materialized path depth 6); chat SignalR; membership | `Communities`, `Posts`, `Memberships` |
| **PricingIntelligence** | Quartz semanal → API ML + admin seed + comunidade + IPCA/IBGE → Ollama Qwen 7B → ref BRL; mediana RVM; sugestão justa | `PriceReferences` |
| **Moderation** | Árbitro de disputas, denúncias, bans, auditoria. Mods de comunidade (escopo) + admins (global) | `Reports` |
| **Notifications** | Push (Web Push/PWA) + in-app SignalR | `Notifications` |
| **Reputation** | Avaliações 1–5, média, níveis/badges | `Reviews` |
| **BlockchainIndexer** | Sync eventos on-chain → read models **idempotentes** (`txHash+logIndex`). Source of truth on-chain | projeções em `Accounts`/`Trades` |

> **Host:** API ASP.NET única (endpoints + auth + SignalR hub). Sem YARP. Reverse proxy: Traefik + Cloudflared.

### Estrutura da solution (`src/`)
```
src/
├─ Revoa.sln
├─ Revoa.Api/                  # host único (endpoints+auth+SignalR hub), /health
├─ modules/
│  ├─ identity/   account/   catalog/   exchange/
│  ├─ token/      community/ pricing/   moderation/
│  └─ notifications/  reputation/  indexer/
│     (cada um: Domain/ Application/ Infrastructure/ — coleção própria)
└─ shared/{Abstractions, Infrastructure, IntegrationContracts}/
```

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
```
PRODUTO:
  Listar(mint NFT→Vault) → Offer → Funded(buyer Block) → Delivered
        → [72h] → Released(RVM −2% → seller, NFT → buyer) | Disputed → árbitro
  Cancelled → Refunded(RVM→buyer, NFT→seller)

SERVIÇO:
  Offer → Funded(RVM block + voucher→buyer) → Redeem/confirm
        → [72h] → Released(RVM −2% → provider, voucher burn) | Disputado → árbitro
  Expiry(30d) → Refunded(RVM→buyer)
```

### Consistência on/off-chain
- **Estado financeiro autoritativo ON-CHAIN.** O Indexer projeta eventos → read models idempotentes.
- **Ações do usuário = UserOperations** (assinadas pela Safe); **admin = ops autorizadas** (role `ARBITRATOR`/`MINTER`).
- **Reorgs:** aguardar N confirmações antes de considerar final; Indexer reprocessa por `txHash+logIndex`.
- **Doação/voluntariado:** reusam o EscrowVault/voucher com **valor 0** (mesma atomicidade; sem RVM do receptor). Recompensa multi-eixo (reputação + bônus RVM admin + pontos) creditada off-chain ao doador/voluntário.

### Ferramental Foundry
- `forge test` (unit, todos os estados) · `forge coverage` (≥90% em EscrowVault/RVM/NFT/Voucher/Paymaster/CouponRedeemer) · `anvil` (nó local p/ Testcontainers).

---

## 6. Account Abstraction (carteira invisível)

> Stack: **Safe + módulo 4337** + **EntryPoint canônico** + **bundler Stackup** + **verifying paymaster** + **Coinbase `webauthn-solidity`** (signer P-256).

### Onboarding UX (carteira invisível)
1. Usuário cria conta com **email + passkey (WebAuthn)** — **sem seed phrase**.
2. Backend (via bundler) cria uma **Safe** com módulo 4337 + signer `webauthn-solidity`.
3. **Faucet R$20** minta RVM na nova Safe.
4. Recuperação: **admin + timelock** (privado). Futuro público: **guardians (M-de-N)**.
5. **Gas invisível**: o Paymaster patrocina; o usuário só vê RVM.

### Interação com a chain
- Toda ação on-chain do usuário é uma **UserOperation** (assm pela Safe, enviada ao bunder Stackup).
- O Paymaster assina off-chain (verifying); o contrato Paymaster verifica on-chain.
- O **Indexer** escuta eventos do EntryPoint + contratos → atualiza saldos/estados off-chain.

---

## 7. BlockchainIndexer (source of truth)

- Consome eventos on-chain (`Transfer`, `Escrow*`, `Mint`, `Burn`, `Redeem`, `CouponRedeemed`...).
- Projeção **idempotente**: chave composta `txHash + logIndex` (nunca aplica o mesmo evento 2×).
- Read models em `Accounts` (saldos) e `Trades` (estado do escrow) — **consultados pelo app**; o saldo on-chain é a verdade, o read model é o cache de leitura.
- Restart-safe: rastreia último bloco processado; reprocessa em caso de gap.
- Confirmações: eventos só "finalizam" o fluxo de UI após N confirmações.

---

## 8. Frontend (React + TypeScript SPA + PWA)

> Stack: **React + TypeScript (Vite SPA) + Tailwind + `viem` + `permissionless.js` + Safe SDK + SignalR client + PWA.** (Único backend: .NET; único framework frontend: React/TypeScript.)

- **Carteira invisível:** passkey → cria Safe (permissão via Safe SDK + permissionless.js p/ UserOps; viem p/ assinar).
- **Feed dinâmico por `kind`:** um objeto Anúncio; o frontend renderiza campos conforme `kind` (ProductDetails vs ServiceDetails).
- **Troca tracker:** acompanha estado do escrow em tempo real (SignalR + Indexer).
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

- **Quartz semanal:** API ML + seed admin + comunidade → normalização **Ollama Qwen 7B (GPU)** → referência BRL por categoria.
- **Mediana RVM** dos listings + **sugestão justa** (faixa RVM).
- **webfetcher trimestral:** IPCA/IBGE → reajusta parâmetros (faucet/cupom + base demurrage).
- **Transparência:** página pública de preços/parâmetros (`PRICING.md`).

---

## 12. Deploy & Cutover

- **Stack central** `projetosia/infra`: 1 túnel Cloudflare + 1 Traefik. Protocolo `infra/README.md`.
- **Dois domínios:** `revoa.me` = **app/plataforma** (este host); `revoa.org` = **blog/docs/painel de transparência/impacto** (site institucional sem fins lucrativos, servido como conteúdo estático/blog — pode ser o mesmo app com rota dedicada ou site separado na Fase 4).
- **revoa.me** hoje roteia (file-provider `routers-revoa.yml`) para `revoa-app:8000` (atualmente o Python trocadeira).
- **Cutover (Fase 4):** parar/remover container trocadeira → o `.NET revoa-app` (mesmo nome + porta) assume `revoa.me`/`www.revoa.me`. `revoa.org`/`www.revoa.org` aponta para o app (rota `/transparencia`/blog) ou site institucional. **Sem mudança de DNS** (Cloudflare→túnel→Traefik→labels).
- **Regras (NUNCA violar):** sem portas no host; sem docker socket; DB/chain só na rede `internal`; públicos na `traefik_net`; `/health` obrigatório; confiar em `X-Forwarded-*`; app em `0.0.0.0`.

---

## 13. Observabilidade & Testes

- **OpenTelemetry + Serilog.** Tracing distribuído (API → Indexer → chain).
- **Testes:** xUnit + FluentAssertions + Testcontainers (Mongo + anvil) por handler/módulo; isolamento de coleção enforced; optimistic locking (Concurrent→ConcurrencyException). Foundry (`forge test`/`coverage`). Playwright (e2e). `workers:1` p/ e2e autenticados.
- **CI verde:** `dotnet build` + `dotnet test` + `forge build` + `forge test` + `npm run build` + `npm run typecheck`.

---

*Referência técnica-fonte-de-verdade. Sucessor da modelagem do `equivale`. Para regras operacionais: `BUSINESS_RULES.md`. Para decisões: `docs/decisions/`.*
