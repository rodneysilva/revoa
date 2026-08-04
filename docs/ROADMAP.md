# ROADMAP.md — Fases do revoa.me

> **Plano de execução por fases.** Detalha o que cada fase entrega (objetos, módulos, contratos,
> fluxos UF), critérios de aceite (DoD), dependências e riscos. Fonte: plano-fonte-de-verdade §11,
> atualizado com as decisões v3.x (sem fins lucrativos, doação primeira classe, cadastro, e-mail auto-hospedado).
> Numeração **UF-NN** e objetos conforme `OOUX.md` / `USER_FLOWS.md`.

---

## Visão geral

| Fase | Nome | Foco principal | Marco | Status |
|------|------|----------------|-------|--------|
| **0** | Direção + Pesquisa + OOUX | Documentação viva, identidade, ADRs | Memória + mapa OOUX + 6 logos comunidade | ✅ Concluída |
| **1** | Infra de chain + Foundation | Contratos base + módulos fundacionais | Usuário cadastra, ganha RVM, tem Safe/saldo | ⏳ Próxima |
| **2** | Trocas + Doação + Comunidades | Núcleo transacional + ajuda mútua | MVP usável: troca, doação/voluntariado, comunidade | ⏳ |
| **3** | Inteligência + Confiança | Pricing, demurrage, cupom on-chain, reputação, moderação | Economia saudável + confiança | ⏳ |
| **4** | Qualidade + Deploy privado | E2E, observabilidade, cutover revoa.me | App em produção privada + revoa.org | ⏳ |

---

## Fase 0 — Direção + Pesquisa + OOUX ✅
> **Status: CONCLUÍDA.** Trabalho não-código (documentação, identidade, decisões).

**Entregues:**
- Skeleton: git + `.gitignore` + `docker-compose.yml` (contrato de deploy; `traefik_net` + interna; profiles app/chain).
- **9 documentos** em `docs/`: BUSINESS, TOKENOMICS, ARCHITECTURE, BUSINESS_RULES, OOUX (26 objetos, 32 fluxos), USER_FLOWS (UF-01..UF-32), MARKET_RESEARCH (economia solidária + Lei 14.478 + benchmark cadastro), VISUAL_IDENTITY, VISUAL_EXPLORATIONS.
- **Skill OOUX** (`.kilo/skills/ooux/SKILL.md`) — processo ORCA.
- **14 ADRs** (`docs/decisions/`).
- **12 explorações visuais** (6 comunidade + 6 Web3/híbridas) + boards de elementos e aplicações web/mobile.
- **README.md** centralizador (índice de continuidade).

**DoD:** documentação viva coerente (sem fins lucrativos, doação primeira classe, cadastro com verificação dupla, e-mail auto-hospedado, 2 domínios). ✔

---

## Fase 1 — Infra de chain + Foundation
> **Objetivo:** usuário consegue se cadastrar (com/sem cupom), ganhar RVM (faucet), ter uma Safe com saldo
> (auto-custódia), e o Indexer projeta o estado on-chain. Tudo em rede local (anvil/Subnet privada).

### Entregáveis
**Infra (`docker-compose.yml`, profile `chain`):**
- Nó Avalanche Subnet-EVM (rede interna); MongoDB replica set `rs0`; MinIO; **Postfix+OpenDKIM** (`revoa-mail`); bundler Stackup; Paymaster; Ollama (Qwen 7B).

**Contratos (`contracts/`, Foundry):**
- `RVM` (ERC-20, roles MINTER/BURNER) · **EntryPoint canônico** (ERC-4337) · **Safe + módulo 4337 + recovery (admin+timelock)** · **Coinbase `webauthn-solidity`** (signer P-256) · **Paymaster (verifying)** · **`CouponRedeemer`** (cupom on-chain, maxUses/expiry).
- `forge test` (todos os caminhos) + `forge coverage` ≥90% em RVM/Paymaster/CouponRedeemer.

**Solution .NET (`src/`):**
- `Revoa.sln` + `shared/` (Abstractions, Infrastructure, IntegrationContracts) + `Revoa.Api` (host único: endpoints + JWT + SignalR hub + `/health`).
- **Isolamento de módulos** enforce (coleções próprias; **teste que falha em query cross-coleção**).

**Módulos:** **Identity** (UF-02/03/04/05) · **Account** (Safe, saldo) · **Token** (faucet R$20→mint; cupom on-chain maxUses) · **BlockchainIndexer** (eventos → read models idempotentes `txHash+logIndex`).

**Serviços de suporte:** **MailKit** (e-mail → Postfix, domínio revoa.me) · **Zenvia** (OTP WhatsApp+SMS) · login social **Google+Apple** · ViaCEP · geocodificação · avatar auto (DiceBear).

### Fluxos cobertos
UF-01 (navegação anônima), UF-02/03 (cadastro sem/com cupom), UF-04 (login), UF-05 (recuperação), UF-06 (onboarding), UF-17 (resgatar cupom), UF-26 (ver carteira, leitura).

### DoD
- Cadastro funciona com/sem cupom; e-mail + WhatsApp verificados; Safe gerada; faucet R$20 (+cupom) creditado; saldo visível (Indexer).
- `forge test` verde; `dotnet build` + `dotnet test` verde; contratos não-redeployáveis (chain ID fixo).
- Containers sobem com `docker compose --profile app --profile chain up -d`.

### Dependências / Riscos
- Depende: chaves Google/Apple OAuth, token Zenvia, DNS revoa.me (MX/SPF/DKIM/DMARC).
- Risco: Safe+4337+webauthn (complexidade) → mitigar com componentes canônicos auditados + security review.

---

## Fase 2 — Trocas + Doação + Comunidades (MVP usável)
> **Objetivo:** MVP usável — o usuário anuncia (5 combinações), troca (atomic swap), **doa/voluntaria**
> (recompensa multi-eixo), participa de comunidades (posts recursivos + chat) — tudo no frontend React.

### Entregáveis
**Contratos:** `ProductNFT` (ERC-721, mintToEscrow) · `ServiceVoucher` (ERC-1155, mint-on-purchase, redeem, expiry 30d) · `EscrowVault` (atomic swap + role ARBITRATOR + janela 72h `block.timestamp` + claim pós-expira + 2%→Fundo Comunitário) · `Treasury` (Fundo Comunitário). `forge coverage` ≥90%.

**Módulos:** **Catalog** (UF-07..11: anúncios kind+VOs, modos, visibilidade, mint, comparativo, metadata MinIO, feed por geolocalização) · **Exchange** (UF-12/13: trocas + estados + Indexer; UF-14/15: **doação/voluntariado** reusando escrow/voucher com valor 0 + recompensa multi-eixo) · **Community** (UF-18..21: default+user, criador+moderadores, posts recursivos materialized path depth 6, chat SignalR, membership) · **Notifications** (UF-32: SignalR + Web Push).

**Frontend React+TS (Vite SPA + PWA):** carteira invisível (passkey→Safe); feed dinâmico por `kind`; **5 formulários de anúncio** (produto trocar/repassar/doar, serviço trocar/voluntariar); troca tracker; doação/voluntariado (fila de pedidos + curadoria); comunidade (posts recursivos + chat); **hero com slogans rotativos**.

**Traefik:** labels + JWT + SignalR (anônimo vê; autenticado age — gate UF-01).

### Fluxos cobertos
UF-07 a UF-15, UF-18 a UF-22, UF-32. (Doação = produto de primeira classe.)

### DoD
- Criar anúncio dos 5 tipos; comprar produto (atomic swap −2%→Fundo); contratar serviço (voucher 30d); **doar produto** (fila→escolha→NFT transfere→recompensa multi-eixo); **voluntariar serviço** (voucher voluntariado→recompensa); posts recursivos (depth 6) + chat ao vivo; navegação anônima + gate de ação.
- `forge test` + `dotnet test` + `npm run build` + `npm run typecheck` + Playwright (smoke) verdes.

### Dependências / Riscos
- Depende: Fase 1 (Safe/Indexer/RVM).
- Risco: atomic swap + estados (todos os caminhos) → fuzzing; posts recursivos + chat RT → materialized path + SignalR.

### Sub-marcos (execução incremental)
| Sub | Foco | Status |
|-----|------|--------|
| **2A — Catalog** | Listing (kind+modo+VOs) + Category + geolocalização (ViaCEP+Haversine) + feed (raio/kind/categoria/comunidade) + comparativo (stub) + mint `ProductNFT` ao listar produto. UF-07..11. | ⏳ em andamento |
| **2B — Community** | Comunidades (default+user), membership (criador/mod/membro), posts recursivos (materialized path depth 6), chat SignalR (90d). UF-18..21. | ⏳ |
| **2C — Exchange** | Trocas (atomic swap) + doação/voluntariado (valor 0, recompensa multi-eixo). Integra `EscrowVault`/`ProductNFT`/`ServiceVoucher`. UF-12..15. | ⏳ |
| **2D — Notifications** | Hub SignalR + Web Push. UF-32. | ⏳ |
| **2E — Frontend** | React+TS SPA+PWA: carteira invisível, feed por kind, 5 forms de anúncio, troca tracker, doação, comunidade, hero slogans rotativos. | ⏳ |

---

## Fase 3 — Inteligência + Econômico + Confiança
> **Objetivo:** economia saudável (pricing, demurrage, cupom) e confiança (reputação, moderação, disputa).

### Entregáveis
**Módulos:** **PricingIntelligence** (UF-28: Quartz semanal → API ML + admin seed + comunidade + IPCA/IBGE trimestral → Ollama Qwen 7B → ref BRL por categoria, mediana RVM, sugestão justa; página de transparência) · **Token** (UF-27: **demurrage execution** keeper IPCA-trimestral + preview/run admin; UF-29: **cupom/convite on-chain** CRUD admin + resgate + rate-limit; UF-30: **parâmetros admin** — `DonationReward:BonusRvm`, taxa, demurrage, faucet) · **Reputation** (UF-23: reviews 1–5, níveis/badges, selos 🎁/🤝, pontos de ajuda) · **Moderation** (UF-24/25: árbitro de disputas, moderadores de comunidade, denúncias/bans/auditoria).

**Contratos:** revisar `CouponRedeemer` (resgate mint); sem novos contratos críticos.

### Fluxos cobertos
UF-23, UF-24, UF-25, UF-27, UF-28, UF-29, UF-30.

### DoD
- Pricing atualiza semanalmente + IPCA trimestral reajusta parâmetros; demurrage queima (idempotente); cupom on-chain resgata (maxUses); reputação + selos de ajuda; moderação resolve disputas/denúncias.
- Comparativo sempre visível; `revoa.org` publica parâmetros/impacto.

### Dependências / Riscos
- Depende: Fase 2.
- Risco: PricingIntelligence (API ML ToS + LLM GPU) → cache + Qwen 7B confirmado; demurrage rejeição → piso generoso + copy clara.

---

## Fase 4 — Qualidade + Deploy privado
> **Objetivo:** produto testado ponta a ponta, observável, em produção privada (revoa.me + revoa.org), PWA instalável.

### Entregáveis
**Testes E2E (Testcontainers Mongo+anvil + Playwright):** cadastro (sem/com cupom)→faucet→saldo; mint NFT→comprar→atomic swap→release (−2%); P2P; voucher→redeem; voucher expira→reembolso; **doação (fila→curadoria→recompensa)**; voluntariado; disputa→árbitro; comunidade (post recursivo + chat ao vivo); cupom on-chain→mint; demurrage; comparativo; recuperação admin+timelock; **navegação anônima + gate**.
**Observabilidade:** OpenTelemetry + Serilog (tracing API→Indexer→chain).
**Deploy:** `docker compose up -d` no stack infra; **cutover** reapontando `revoa.me`/`www.revoa.me` do trocadeira para o app novo (sunset Python); `revoa.org` (blog/transparência); PWA instalável.
**Formalização (pré-público):** documentar "abrir público depois" (validadores, RPC, guardians, subnet mainnet, ARBITRATOR quorum, stablecoin) + iniciar formalização associação/OSC.

### Fluxos cobertos
Todos (UF-01..UF-32) em E2E.

### DoD
- E2E verde (`workers:1` autenticados); CI verde (`dotnet build/test` + `forge build/test` + `npm build/typecheck`); container segue checklist `infra/README.md` (0.0.0.0, X-Forwarded, /health, sem portas host, DB só interna); revoa.me acessível + PWA instalável; revoa.org com transparência.

### Dependências / Riscos
- Depende: Fases 1–3.
- Risco: cutover (parar trocadeira, labels Traefik, Cloudflared) → sem DNS; regulação → MVP privado + parecer jurídico pré-público.

---

## Pós-MVP / Público (fora do escopo atual)
- Subnet L1 de settlement em mainnet (dev = Fuji/local; mainnet no público).
- Quorum ARBITRATOR público (Safe multisig) · guardians (M-de-N) · stablecoin BRL 1:1 (lastro+KYC) · on/off-ramp.
- Nestes casos: reavaliar Lei 14.478 (VASP) + formalizar OSC.

---

## Princípios transversais a toda fase
1. **OOUX primeiro:** toda feature parte do mapa (`OOUX.md`) — objetos antes de telas/fluxos.
2. **Documentar regras:** `BUSINESS_RULES.md` + ADR para cada decisão.
3. **YAGNI:** nenhuma classe/objeto sem uso (CI detecta símbolos sem consumidor).
4. **Isolamento de módulos:** coleções MongoDB próprias; proibido query cross-coleção (evento via MediatR).
5. **Commitar ao final de cada tarefa** (regra do workspace); mensagens pt-BR conventional commits.

---

*Documento-fonte-de-verdade do roadmap. Atualizar conforme fases avançam (marcar ✔ e datas).*
