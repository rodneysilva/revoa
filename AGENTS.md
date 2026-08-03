# AGENTS.md — Memória do Projeto revoa.me

> **Plataforma DeFi de troca de produtos e serviços** — economia circular tokenizada (RVM),
> on-chain, self-custody. **Plano-fonte-de-verdade:**
> [`~/.local/share/kilo/plans/1785722983643-revoa-defi-platform.md`](../../../).
> A memória global está em `~/.config/kilo/AGENTS.md` (PowerShell, MongoDB, PWA, cloudflared).
> Este arquivo complementa com o que é **ÚNICO** do revoa.

---

## Stack

| Camada | Tecnologia |
|--------|------------|
| **Backend** | .NET 10 · Nethereum · MediatR (CQRS) · Quartz.NET · FluentValidation · Mapster · SignalR |
| **Persistência** | MongoDB (replica set `rs0`; optimistic locking + `Version`; ACID seletiva) |
| **Storage** | MinIO (S3-compatible; metadata/imagens NFT; `tokenURI` aponta p/ cá) |
| **Contratos** | Solidity + Foundry (`forge test`, `forge coverage`, `anvil`) |
| **Chain** | Avalanche Subnet-EVM própria (privada agora → pública depois, mesma chain) |
| **Account Abstraction** | ERC-4337: Safe + módulo 4337 + EntryPoint canônico + bundler Stackup + verifying paymaster + Coinbase `webauthn-solidity` (P-256) |
| **Pricing/LLM** | API ML + seed admin + comunidade + webfetcher(IPCA/IBGE) + Ollama Qwen 7B (GPU) |
| **Auth** | JWT + FIDO2/WebAuthn (passkeys) — **carteira invisível** (email + passkey cria Safe) |
| **Frontend** | **React + TypeScript** (Vite SPA) + Tailwind + viem + permissionless.js + Safe SDK + SignalR client + PWA. **Único backend: .NET; único framework frontend: React/TypeScript.** |
| **Reverse proxy/SSL** | Traefik + Cloudflared (infra `projetosia/infra`) |
| **Testes** | xUnit + FluentAssertions + Testcontainers (Mongo+anvil) · Foundry · Playwright |

> **Pivot travado:** o revoa **não** é mais "sem dinheiro" (trocadeira Python). É
> **economia circular e de ajuda mútua, sem fins lucrativos, com moeda social comunitária (RVM)** —
> auto-custódia, on-chain. **Natureza jurídica: projeto informal (sem CNPJ) por ora**; formalizar
> associação/OSC antes do público. Ver `docs/BUSINESS.md` e `docs/MARKET_RESEARCH.md`.
> **Dois domínios:** `revoa.me` (app) · `revoa.org` (blog/transparência/impacto).

---

## Padrões e Convenções IMPORTANTES

> Padrões universais (PowerShell 5.1, MongoDB PascalCase, optimistic locking, CRLF harmless)
> estão no AGENTS.md global. Abaixo apenas o específico do revoa.

### MongoDB (blueprint = equivale)
- **Sem convenção camelCase** no driver C#. Campos serializados em **PascalCase** (`"Version"`, não `"version"`). Exceção: `_id`.
- **Optimistic locking:** toda entidade tem `public long Version { get; set; }`. `UpdateAsync` filtra por `_id + Version == expected`; `MatchedCount == 0` → `ConcurrencyException`.
- **Docs legados:** tratar com `$or` + `$exists: false` (campo `Version` ausente).
- **ACID seletiva:** `IUnitOfWork.ExecuteInTransactionAsync` envolve **APENAS** a finalização financeira (`FinishTransactionAsync` — libera/queima RVM). Demais writes (create/cancel) usam optimistic locking + escrow.
- **Anti-N+1 (embed de nomes):** documentos de Listing embedam `SellerName`/`SellerAvatarUrl`/`CommunityName` (populados no command handler ao criar). `DtoEnricher` = fallback p/ legados.
- **Isolamento de módulos:** cada bounded context tem **coleções próprias**; **PROIBIDO** query cross-coleção. Comunicação só via `IIntegrationEventBus` (in-process → MassTransit ao extrair). Teste deve falhar ao detectar query cross-coleção.

### On-chain ↔ Off-chain
- **Estado financeiro autoritativo ON-CHAIN.** O Indexer projeta eventos → read models **idempotentes** (chave `txHash + logIndex`).
- **Ações do usuário = UserOperations (Safe); ações admin = ops autorizadas (role ARBITRATOR).**
- **Mint-to-escrow:** `ProductNFT.mintToEscrow()` ao **listar** (NFT nasce dentro do EscrowVault, nunca toca a carteira do vendedor). Atomic swap move só RVM na compra.
- **ServiceVoucher:** mint-**on-purchase** (voucher mintado para a Safe do comprador ao pagar RVM; queimado no `redeem`).
- **Reorgs:** aguardar confirmações antes de considerar final.

### Fluxo Git / Ambientes (herdar padrão equivale)
- **Branches:** `master` (canônica) · `dev` (desenvolvimento ativo) · `hom` (homologação via worktree).
- **SEMPRE commitar + push ao final de cada tarefa** (regra do dono, não esperar pedir).
- Antes de commitar: rode lint/typecheck/build + `git status` + `git diff`.
- Mensagens em **português**, conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `infra:`, `chore:`.

---

## Bounded Contexts (12 módulos do monólito)

| Módulo | Responsabilidade | Coleções MongoDB próprias |
|--------|------------------|---------------------------|
| **Identity** | Registro (cupom **opcional** + confirmação **e-mail e WhatsApp/telefone** + login **Google/Apple**), WebAuthn/passkeys, JWT, roles, recuperação admin+timelock. **E-mail: MailKit→Postfix** (revoa.me, ADR-0014); **WhatsApp/SMS: Zenvia** | Users |
| **Account (Wallet)** | Smart accounts (Safe) 4337, saldo (read model Indexer), transfer P2P, Paymaster/Bundler | Accounts |
| **Catalog** | Anúncios (kind + VOs), categorias, **modo** (trocar/repassar/doar/voluntariar), **visibilidade**, mint on-chain, metadata (MinIO), busca, comparativo de preço, feed por geolocalização | Listings, Categories |
| **Exchange** | 3 fluxos + máquina de estados do escrow (atomic swap) + disputa + Indexer (source of truth) | Trades |
| **Token (Treasury)** | RVM mint/burn, faucet R$20, demurrage (IPCA-trimestral), taxa 2%→**Fundo Comunitário** (sem FLP), cupom/convite on-chain (maxUses), **bônus de doação (admin-configurável)** | (on-chain; ledger mirror) |
| **Community** | Default + user-created; criador+moderadores+membros; posts recursivos (materialized path, depth 6); chat SignalR; membership com roles | Communities, Posts, Memberships |
| **PricingIntelligence** | Quartz **semanal** → API ML + admin seed + comunidade + webfetcher IPCA/IBGE trimestral → Ollama Qwen 7B → referência BRL por categoria; mediana RVM; sugestão justa | PriceReferences |
| **Moderation** | Árbitro de disputas (escrow), denúncias, bans, auditoria. Mods de comunidade agem em anúncios/posts da própria comunidade; admins global | Reports |
| **Notifications** | Push (Web Push/PWA) + in-app SignalR: escrow/ofertas/transferências/posts/chat/preços | Notifications |
| **Reputation** | Avaliações 1–5, média, níveis/badges | Reviews |
| **BlockchainIndexer** | Sync eventos on-chain → read models **idempotentes** (txHash+logIndex). Source of truth on-chain | (projeções em Accounts/Trades) |

**Host:** API ASP.NET única (endpoints + auth + SignalR hub). Sem YARP.

---

## Infra / Deploy

- **Stack central:** `projetosia/infra` (1 túnel Cloudflare + 1 Traefik). Protocolo em `infra/README.md`.
- **Roteamento revoa.me:** file-provider em `infra/traefik/dynamic/routers-revoa.yml` → `http://revoa-app:8000`.
  O container `app` **DEVE** se chamar `revoa-app` e escutar em `0.0.0.0:8000`.
- **Cutover (Fase 4):** parar/remover container Python do trocadeira (atual `revoa-app`) → o .NET assume `revoa.me`/`www.revoa.me` automaticamente. `revoa.org` já redireciona. **Sem mudança de DNS** (Cloudflare → túnel → Traefik → labels/file).
- **Regras NUNCA violar:** sem portas no host; sem docker socket; DB/chain SÓ na rede `internal`; públicos (app/frontend/minio) na `traefik_net` (external); `/health` obrigatório; confiar em `X-Forwarded-*`.

### Subir ambientes
```powershell
# Infra (uma vez por boot)
cd C:\Users\rodne\projetosia\infra
docker network create traefik_net   # só na 1ª vez
docker compose up -d                # Traefik + cloudflared

# revoa — apenas infra-base (mongo+minio) hoje
cd C:\Users\rodne\projetosia\revoa
docker compose up -d                # sobe serviços sem profile (mongo, minio)
# Fase 1+ (com Dockerfiles):
docker compose --profile app up -d --build
docker compose --profile chain up -d
```

---

## Armadilhas Conhecidas

1. **PowerShell:** `$pid`, `$args`, `$host`, `$input` são reservados — use `$productId`, etc. Use `;` (não `&&`).
2. **MongoDB PascalCase:** filtros manuais usam PascalCase (`"Version"`), só `_id` é underscore.
3. **Ownership:** `SellerId`/`ProviderId`/`BuyerId` vêm do **token**, nunca do body (controller injeta).
4. **Mint-to-escrow ≠ mint-on-purchase:** produto (NFT) ao listar; serviço (voucher) ao comprar. Lifecycles opostos — não misturar.
5. **Isolamento de coleção:** nunca importar repository de outro módulo. Use eventos.
6. **workers: 1** em testes e2e autenticados (carteira compartilhada do admin + optimistic locking).
7. **Marcador E2E:** todo dado de teste (listing/post/comunidade/trade) deve conter **"E2E"** no nome/conteúdo para o script de limpeza.
8. **RVM NÃO é ativo financeiro:** nunca usar "cripto/investir/valorizar". RVM = "crédito de troca". Alinhado à Lei 14.478.
9. **CRLF/LF:** warnings do git são harmless.

---

## Decisões Travadas (resumo — ver Plano §1)

**Chain/Contratos:** Subnet-EVM própria · produto=ERC-721 mint-to-escrow, serviço=ERC-1155 mint-on-purchase · RVM=ERC-20 gas invisível (Paymaster) · AA: Safe+4337+Stackup+Coinbase webauthn (P-256) · recuperação admin+timelock (privado) / guardians (público) · escrow janela 72h `block.timestamp` + role ARBITRATOR · voucher 30d auto-reembolso · cupom ON-CHAIN (CouponRedeemer).

**Tokenomia:** RVM não atrelado ao BRL · IPCA(+INPC) reajusta PARÂMETROS trimestralmente (faucet/cupom + base demurrage + **bônus de doação**) · comparativo duplo (BRL ML+seed+comunidade ↔ mediana RVM) · Ollama Qwen 7B normaliza · faucet R$20 + cupom/convite + mint admin · demurrage 0,5%/mês (piso R$100, queima) · **taxa 2% → Fundo Comunitário (sem fins lucrativos)** · anti-sybil (1/dispositivo, 5/IP/dia, maxUses) · **doação/voluntariado = recompensa multi-eixo (reputação + bônus RVM admin-configurável + pontos de ajuda)**.

**Arquitetura:** monólito modular (coleções isoladas, MediatR) · MongoDB global · Anúncio único com `kind` (product|service) + VOs · Comunidades no MVP (default+user, posts recursivos materialized path depth 6, chat SignalR, geolocalização) · **React+TypeScript SPA + PWA**, carteira invisível · MinIO storage · Traefik+Cloudflared · **backend único .NET; frontend único React/TypeScript**.

**Negócio:** **sem fins lucrativos** (natureza jurídica informal por ora; formalizar OSC pré-público) · **dois domínios** (`revoa.me`=app, `revoa.org`=blog/transparência) · narrativa "moeda social comunitária + ajuda mútua" (herdeira do Banco Palmas, NÃO cripto-investimento).

**Cadastro & acesso:** cupom **opcional** (sem cupom→faucet R$20; com cupom→R$20+cupom) · confirmação dupla **e-mail (MailKit+Postfix, revoa.me) + WhatsApp/SMS (Zenvia)** · **idade 18+** · **CPF opcional** · login social **Google+Apple** · avatar **auto-gerado** · **aberto p/ navegar, fechado p/ agir** (anônimo vê tudo, ação exige login+verificação). Anti-sybil: 1/dispositivo, 5/IP/dia, e-mail+telefone únicos. Ver ADR-0013/0014.

---

## Documentação (memória viva — `docs/`)

| Documento | Conteúdo |
|-----------|----------|
| [BUSINESS.md](docs/BUSINESS.md) | Proposta de valor, personas, mercado, pivot tokenizado |
| [TOKENOMICS.md](docs/TOKENOMICS.md) | RVM, emissão, demurrage, IPCA, taxa, comparativo |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Monólito modular, MongoDB, AA, Indexer, contratos |
| [BUSINESS_RULES.md](docs/BUSINESS_RULES.md) | 3 fluxos, escrow, **doação/voluntariado (multi-eixo)**, comunidades, geolocalização, reputação |
| [USER_FLOWS.md](docs/USER_FLOWS.md) | Jornadas + estados + Gherkin (registro, anúncios, carteira, doação, troca, comunidade) |
| [OOUX.md](docs/OOUX.md) | Mapa de objetos ORCA (objects-first) |
| [MARKET_RESEARCH.md](docs/MARKET_RESEARCH.md) | TAM/SAM/SOM + competidores + cap DeFi/Web3 + Lei 14.478 |
| [VISUAL_IDENTITY.md](docs/VISUAL_IDENTITY.md) | Marca, wordmark "revoa.me", monograma "RV", "RM$" |
| [PRICING.md](docs/PRICING.md) | *(Fase 3)* Intelligence de preços, transparência |
| [ROADMAP.md](docs/ROADMAP.md) | *(a criar)* Fases 0–4 |
| [decisions/](docs/decisions/) | ADRs |

---

## Princípios (Plano §22–24, §2)

1. **SOLID + Clean Code + DDD + Clean Architecture** por módulo; **aggregates alinhados aos objetos OOUX**.
2. **YAGNI estrito** ("nenhuma classe sem uso") + gate de review + documentar todas as regras.
3. **OOUX objects-first (ORCA)** em toda feature nova — partir do mapa `docs/OOUX.md`.
4. **"Frameworks conhecidos/reconhecidos"** — sem stack exótica.
5. **"Foco no que o usuário tem a oferecer (produto/serviço); a economia RVM é meio, não fim."**
6. pt-BR doc/UI; inglês no código.

---

*Última atualização: Fase 0 (03/08/2026).*
