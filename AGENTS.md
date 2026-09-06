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
| **Backend** | .NET 10 · Nethereum · MediatR (CQRS, shared kernel ADR-0016) · FluentValidation · SignalR · Serilog + OpenTelemetry |
| **Persistência** | MongoDB (replica set `rs0`; optimistic locking + `Version`; ACID seletiva) |
| **Storage** | MinIO (S3-compatible; metadata/imagens NFT; `tokenURI` aponta p/ cá) |
| **Contratos** | Solidity + Foundry (`forge test`, `forge coverage`, `anvil`) — 6 contratos, 67 testes |
| **Chain** | Subnet-EVM/anvil privada agora → pública depois (ADR-0001) |
| **Account Abstraction** | **Roadmap (ADR-0002)** — hoje: EOA plaintext managed pelo backend (desvio formalizado no **ADR-0015**; blocker pré-público) |
| **Pricing/LLM** | API ML + seed admin + comunidade + webfetcher(IPCA/IBGE) + Ollama Qwen 7B (GPU); recálculo via endpoint admin (`POST /api/pricing/refresh`) |
| **Auth** | JWT passwordless (código por e-mail; MailKit→Postfix ADR-0014) + dev-login só em Development; **carteira invisível** gerenciada pelo backend |
| **Frontend** | **React + TypeScript** (Vite SPA) + Tailwind + SignalR client + PWA. **Único backend: .NET; único framework frontend: React/TypeScript.** (viem/Safe SDK entram com a AA) |
| **Reverse proxy/SSL** | Traefik + Cloudflared (infra `rodne/infra` — local `C:\Users\rodne\infra`) |
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
- **Estado financeiro autoritativo ON-CHAIN.** O backend consulta a chain via Nethereum (Indexer dedicado é roadmap).
- **Ações do usuário = assinadas pelo backend com a EOA managed (ADR-0015); ações admin = role `ARBITRATOR` (ADR-0018).**
- **Mint-to-escrow:** `ProductNFT.mintToEscrow()` ao **listar** (NFT nasce dentro do EscrowVault, nunca toca a carteira do vendedor). Atomic swap move só RVM na compra.
- **ServiceVoucher:** mint-**on-purchase** (voucher mintado para a Safe do comprador ao pagar RVM; queimado no `redeem`).
- **Reorgs:** aguardar confirmações antes de considerar final.

### Fluxo Git / Ambientes (herdar padrão equivale)
- **Branches:** `master` (canônica) · `dev` (desenvolvimento ativo) · `hom` (homologação via worktree).
- **SEMPRE commitar + push ao final de cada tarefa** (regra do dono, não esperar pedir).
- Antes de commitar: rode lint/typecheck/build + `git status` + `git diff`.
- Mensagens em **português**, conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `infra:`, `chore:`.

---

## Bounded Contexts (13 módulos do monólito — `src/modules/`)

| Módulo | Responsabilidade | Coleções MongoDB próprias |
|--------|------------------|---------------------------|
| **Identity** | Registro (cupom **opcional** + verificação **e-mail e telefone**), JWT passwordless, roles (User/Arbitrator/Admin), recuperação. **E-mail: MailKit→Postfix** (revoa.me, ADR-0014) | Users |
| **Account (Wallet)** | Carteiras EOA por usuário (managed, ADR-0015), saldo, transfer P2P | Accounts |
| **Catalog** | Anúncios (kind + VOs), categorias (seed canônico `CategorySeed`), **modo** (trocar/repassar/doar/voluntariar), **visibilidade**, comentários recursivos, feed por geolocalização + busca | Listings, Categories, Comments |
| **Exchange** | Máquina de estados do escrow (purchase/redeem/release/dispute/cancel/resolve), fila de doação/voluntariado (help requests) | Trades, HelpRequests |
| **Token (Treasury)** | Faucet R$20, mint/burn RVM, taxa 2%→**Fundo Comunitário** | (on-chain; sem coleção própria) |
| **Coupon** | Cupom on-chain (criação admin, resgate minta RVM, revogação) | Coupons |
| **Demurrage** | Demurrage IPCA-trimestral (preview/run, queima) | DemurrageRuns |
| **Community** | Default + user-created; criador+moderadores+membros; posts recursivos (materialized path, depth 6); chat SignalR | Communities, Memberships, Posts, Chats |
| **Pricing** | Referência de preço justo por categoria (mediana comunitária + BRL seed + IPCA/IBGE + Ollama) | PriceReferences |
| **Moderation** | Denúncias + resolução admin (ban via evento → Identity) | Reports |
| **Notifications** | In-app + Web Push: escrow/ofertas/posts/chat/preços | Notifications, PushSubscriptions |
| **Reputation** | Avaliações 1–5 pós-troca, agregados por usuário | Reviews, Reputations |
| **Admin** | Parâmetros runtime tipados (SystemParameters) compartilhados entre módulos | SystemParameters |

> **BlockchainIndexer** (projeção idempotente de eventos → read models) é **roadmap**, não existe
> como módulo — hoje o backend consulta a chain diretamente via Nethereum. Ações admin on-chain
> usam a role `ARBITRATOR` (ADR-0018).

**Host:** API ASP.NET única (endpoints + auth + SignalR hub). Sem YARP.

---

## Infra / Deploy

- **Stack central:** `rodne/infra` (local `C:\Users\rodne\infra`) — PaaS genérico env-driven: Traefik v3 + cloudflared + Portal; routers/túneis GERADOS do `.env` via `scripts/render.py` (nenhum hostname/chave no repo). Protocolo em `infra/README.md`.
- **Roteamento revoa.me:** routers Traefik gerados do `.env` da infra central → `http://revoa-app:8000`.
  O container `app` **DEVE** se chamar `revoa-app` e escutar em `0.0.0.0:8000`.
- **Cutover (Fase 4):** parar/remover container Python do trocadeira (atual `revoa-app`) → o .NET assume `revoa.me`/`www.revoa.me` automaticamente. `revoa.org` já redireciona. **Sem mudança de DNS** (Cloudflare → túnel → Traefik → labels/file).
- **Regras NUNCA violar:** sem portas no host; sem docker socket; DB/chain SÓ na rede `internal`; públicos (app/frontend/minio) na `traefik_net` (external); `/health` obrigatório; confiar em `X-Forwarded-*`.
- **Acesso ao `dev.revoa.org` (PÚBLICO):** o WAF Cloudflare de IP-allowlist está **DESATIVADO** (regra `a51743736c0244c1be07c742010ba6a8` na zona `revoa.org`, phase `http_request_firewall_custom`). Qualquer IP acessa; a **própria app protege as ações** (login + verificação e-mail/telefone) — modelo "aberto p/ navegar, fechado p/ agir". Múltiplas pessoas acessam sem cadastro de IP.
  - **Reativar bloqueio por IP (se necessário):** `PUT /zones/f49fd96f1495cd0b1ef89918c63fef05/rulesets/phases/http_request_firewall_custom/entrypoint` com `enabled:true` no body. Última allowlist: IPv4 `177.197.0.0/16` (ISP Vivo) + IPv6 `/64` (`2804:1b3:aa42:a045::/64`, `2804:1b3:aa43:3d99::/64`). IPs rotacionam (ISP), então o `/16` reduz — mas não elimina — a recorrência.
  - **Credenciais Cloudflare:** `infra/.env` (`CF_API_EMAIL` + `CF_API_KEY` = Global API Key, acesso total). O `CLOUDFLARE_API_TOKEN` (`cfut_...`) é **só de túnel** — NÃO tem permissão de WAF (403).
- **Zero Trust (Cloudflare Access) NÃO provisionado** — a API retorna `access.api.error.not_enabled`; exige 1 clique manual no dashboard ("Enable Access" + escolher team domain). É o caminho recomendado para acesso de múltiplas pessoas sem depender de IP (OTP por e-mail ou Google). Após provisionar, automatiza-se via API (IdP + app + policy).
- **`revoa.org` (blog/apex) com erro 530** conhecido: o Cloudflare não alcança a origin do blog (container/blog não está rodando). Não é bloqueio WAF. `dev.revoa.org` (app) funciona.

### Subir ambientes
```powershell
# Infra (uma vez por boot)
cd C:\Users\rodne\infra
docker network create traefik_net   # só na 1ª vez
docker compose up -d                # Traefik + Portal (+ túneis: docker-compose.tunnels.yml)

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
| [USER_FLOWS.md](docs/USER_FLOWS.md) | **32 fluxos (UF-01..UF-32)**: jornadas + estados + Gherkin (cadastro sem/com cupom, 5 tipos de anúncio, troca, doação, voluntariado, comunidade, admin) |
| [OOUX.md](docs/OOUX.md) | Mapa ORCA **completo: 26 objetos + 32 fluxos mapeados** → aggregates DDD |
| [ROADMAP.md](docs/ROADMAP.md) | **5 fases detalhadas (0–4)**: entregáveis, fluxos, DoD, dependências, riscos |
| [MARKET_RESEARCH.md](docs/MARKET_RESEARCH.md) | TAM/SAM/SOM + competidores + cap DeFi/Web3 + Lei 14.478 |
| [VISUAL_IDENTITY.md](docs/VISUAL_IDENTITY.md) | Marca, wordmark "revoa.me", monograma "RV", "RM$" |
| [decisions/](docs/decisions/) | ADRs (0001–0018) |

---

## Princípios (Plano §22–24, §2)

1. **SOLID + Clean Code + DDD + Clean Architecture** por módulo; **aggregates alinhados aos objetos OOUX**.
2. **YAGNI estrito** ("nenhuma classe sem uso") + gate de review + documentar todas as regras.
3. **OOUX objects-first (ORCA)** em toda feature nova — partir do mapa `docs/OOUX.md`.
4. **"Frameworks conhecidos/reconhecidos"** — sem stack exótica.
5. **"Foco no que o usuário tem a oferecer (produto/serviço); a economia RVM é meio, não fim."**
6. pt-BR doc/UI; inglês no código.

---

*Última atualização: 06/09/2026 — refactoração de coerência: stack real (sem Quartz/MassTransit/Mapster/FIDO2/viem/Safe SDK — são roadmap), 13 módulos reais, ADRs 0015–0018 (EOA desvio, shared kernel, contrato PascalCase+ApiError, role ARBITRATOR).*
