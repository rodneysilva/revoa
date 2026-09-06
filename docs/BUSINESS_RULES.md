# BUSINESS_RULES.md — revoa.me

> **Regras de negócio vivas.** Fonte de verdade operacional. Cada regra deve ser **documentada**
> (princípio YAGNI + "documentar todas as regras"). Atualizar a cada mudança.
> Referências: Plano §8 · `BUSINESS.md` · `TOKENOMICS.md` · `ARCHITECTURE.md`.

---

## 1. Anúncio (Listing)

### 1.1 kind (produto | serviço)
Todo anúncio tem **um** `kind`. O usuário pensa em "uma coisa que oferece" (OOUX) — o formulário
renderiza campos dinâmicos conforme o kind:

| kind | Value Object | Campos |
|------|--------------|--------|
| **product** | `ProductDetails` | `condition` (novo/seminovo/usado), `stock`/`units`, `nftTokenId` (preenchido ao listar) |
| **service** | `ServiceDetails` | `unitType` (`per-service` \| `hours`), `duration`, `voucherExpiry` (30d default) |

### 1.2 modo (trocar / repassar / doar / voluntariar)
| modo | kind | preço RVM | efeito | recompensa ao ofertante |
|------|------|-----------|--------|--------------------------|
| **trocar** | product · service | valor justo (RVM) | mercado RVM | RVM + reputação |
| **repassar** | product | RVM baixo | acessibilidade | RVM + reputação |
| **doar** | product | **0 RVM** | ajuda mútua | **reputação + selo de doador + bônus RVM (admin) + pontos de ajuda** |
| **voluntariar** | service | **0 RVM** | ajuda mútua | **reputação + selo de voluntário + bônus RVM (admin) + pontos de ajuda** |

### 1.3 visibilidade
- `comunidade` (só membros de uma comunidade veem) · `global` (toda a plataforma) · `ambos`.
- Anúncio com `communityId` + visibilidade `comunidade`/`ambos` aparece no feed da comunidade.

### 1.4 preço + comparativo
- Preço em **RVM** sempre (exceto doar/voluntariar = 0).
- **Comparativo sempre exibido:** BRL de mercado (ML+seed+comunidade) ↔ mediana RVM da categoria ↔ sugestão justa (Ollama).
- O comparativo é **informativo**, não cotação garantida.

### 1.5 geolocalização
- Todo anúncio tem `location`: `lat`, `lng`, `bairro`, `cidade` (via **ViaCEP** + **HTML5 geolocation**).
- Feed suporta filtro por **raio**: 1 / 5 / 10 / 25 km (cálculo **Haversine**).
- Comunidades default existem **por cidade**.

### 1.6 tokenização (mint)
| kind | quando minta | destino |
|------|--------------|---------|
| **product** | ao **listar** (`mintToEscrow`) | NFT nasce no **EscrowVault** (nunca na carteira do vendedor) |
| **service** | ao **comprar** (`mint-on-purchase`) | voucher → carteira (Safe) do comprador |

> Não há NFT de serviço até a compra. O serviço é "virtual" até virar voucher.

---

## 2. Escrow & Máquinas de Estado

### 2.1 PRODUTO
```
Listar (mint NFT → Vault)
  → Offered  (comprador manifesta interesse)
  → Funded (comprador paga RVM → buyer.Block(total); NFT segue no Vault)
  → [janela 72h, block.timestamp]
       ├─ Released  → atomic swap: seller.Credit(total − 2%) + treasury.Credit(2%) + NFT→buyer
       └─ Disputed → árbitro (ARBITRATOR) decide (libera/reembolsa)
  Cancelled (antes do release) → Refunded: buyer.Unblock(total) + NFT→seller
```

### 2.2 SERVIÇO
```
Offer
  → Funded (buyer.Block(RVM) + voucher mintado → buyer)
  → Redeem/confirm (comprador usa o serviço / confirma)
  → [janela 72h]
       ├─ Released → seller.Credit(total − 2%) + treasury.Credit(2%) + voucher.burn()
       └─ Disputed → árbitro decide
  Expiry (30d sem redeem) → Refunded: buyer.Unblock(RVM)
```

### 2.3 Regras do escrow
- **Estados reais do aggregate `Trade`:** Offered · Funded · Released · Disputed · Refunded · Cancelled (espelham o `EscrowVault`). **Não há estado "entregue"** — a logística é combinada fora da plataforma; a liberação é cooperativa (vendedor **ou** comprador) a qualquer momento após Funded, ou automática após 72h sem disputa. No serviço, o `redeem` do voucher é um flag (`VoucherRedeemed`), não um estado.
- **Fundos bloqueados** (`Block`) ao pagar; **liberados/creditados** só na finalização (`FinishTransactionAsync`, dentro de transação ACID).
- **Taxa 2%** do vendedor → **Fundo Comunitário** (sem fins lucrativos; reinvestido na operação).
- **Janela 72h** medida em `block.timestamp` (on-chain, imutável).
- **ARBITRATOR** (admin/Safe plataforma) é a única role que resolve disputa.
- **Validade voucher 30d** → auto-reembolso + claim do provider via árbitro.

### 2.4 Fluxo de Doação (produto de primeira classe)

> **Doação é central**, não periférica. Coexiste com a troca em RVM — o usuário **escolhe** receber
> crédito de troca **ou** ajudar de graça. Reusa o escrow/voucher com **valor 0** (consistência, atomicidade).
> Inspirada no **Buy Nothing Project** (dar + pedir + gratidão).

#### 2.4.1 Doar (produto)
```
Anunciar Doar (0 RVM) → ProductNFT mintado no EscrowVault (mesmo mint-to-escrow do Trocar)
  → Pedidos: interessados manifestam interesse (fila de pedidos c/ mensagem)
  → Doador escolhe receptor (curadoria humana: proximidade, reputação, mensagem)  ← matching da doação
  → Receptor aceita (0 RVM) → Entregue → Confirmado → NFT transfere ao receptor
  → Recompensa multi-eixo ao DOADOR (reputação + selo de doador + bônus RVM + pontos de ajuda)
Cancelado (antes de aceitar) → NFT devolvido/queimado do Vault; sem recompensa
```

#### 2.4.2 Voluntariar (serviço)
```
Anunciar Voluntariar (0 RVM) → sem voucher até o aceite
  → Pedidos: interessados pedem ajuda
  → Voluntário escolhe quem ajudar (curadoria)
  → "Contratar" (0 RVM) → voucher de voluntariado (ERC-1155) mintado p/ rastreabilidade
  → Prestado → Redeem/Confirm → voucher queimado
  → Recompensa multi-eixo ao VOLUNTÁRIO (reputação + selo de voluntário + bônus RVM + pontos de ajuda)
Cancelado/Expirado → sem recompensa
```

#### 2.4.3 Recompensa multi-eixo (configurável)
| Eixo | Mecânica | Configurável |
|------|----------|--------------|
| **Reputação/badges** | avaliação + selos de doador/voluntário (perfil) | thresholds admin |
| **Bônus de RVM** | faucet extra creditado ao doador/voluntário | **`DonationReward:BonusRvm` por kind/modo — admin** |
| **Pontos de ajuda** | contador separado, não conversível; ranking comunitário | peso admin |

> O bônus de RVM é **parâmetro administrativo** — o admin calibra o incentivo à ajuda sem mudar código.
> Doação/voluntariado **não movimenta RVM do receptor** (preço 0) e **não paga taxa**.

---

## 3. Os 3 Fluxos Econômicos
1. **Moeda → Anúncio:** comprar produto/serviço com RVM (atomic swap).
2. **Anúncio → Moeda:** oferecer e receber RVM (ao liberar).
3. **Moeda → Conta:** transferência **P2P** de RVM entre carteiras (inclui "presentear" RVM de graça) — *(roadmap: ainda não há endpoint de transferência)*.

> **Doação/voluntariado NÃO são fluxos econômicos** (não movimentam RVM do receptor) — são **fluxos de
> ajuda** com recompensa própria (multi-eixo). Detalhe em §2.4 e `USER_FLOWS.md`.

---

## 4. Comunidades

### 4.1 Tipos
- **Default** (plataforma): uma por **cidade** (auto-vinculada por localização no onboarding).
- **User-created:** criador = admin. **Open** (qualquer um entra) ou **private** (password-gated).

### 4.2 Eixos (operam simultaneamente — usuário pertence a várias sem fricção)
- **Geográfica** (condomínio/bairro/cidade/região) — eixo **principal**.
- **Por interesse** (categoria/nicho: gamers, mães, estudantes, vinis, plantas...).
- **Por causa** (ESG/sustentabilidade/zero-waste).

### 4.3 Hierarquia de papéis (na comunidade)
| papel | permissões |
|-------|-----------|
| **criador** (admin da comunidade) | tudo da comunidade + nomear moderadores + editar/excluir |
| **moderador** | aprovar/rejeitar anúncios + moderar posts/chat **da própria comunidade** |
| **membro** | ver/postar/anunciar (conforme regras), comentar, chat |

> Moderadores de comunidade agem **só** no escopo da comunidade. Admins globais agem em toda a plataforma.

### 4.4 Posts recursivos
- **Materialized path**, profundidade **6** (resposta a resposta... até 6 níveis).
- **Moderação em cascata:** ocultar/excluir um post propaga aos descendentes (sem replies órfãos).
- `CreatePostDto`: `CommunityId`/`AuthorId` **nullable** (controller injeta do token/rota).

### 4.5 Chat
- **Chat geral da comunidade** em tempo real (**SignalR**).
- Retenção **90 dias**.
- Moderado **como posts** (denúncia/ocultar/ban aplicam igual).

### 4.6 Geolocalização das comunidades
- `lat`/`lng` via **ViaCEP** (CEP→endereco) + **HTML5 geolocation** (consentimento).
- Feed por **raio** 1/5/10/25 km (**Haversine**).
- Comunidade default por cidade (auto-vínculo no cadastro).

---

## 5. Cadastro & Onboarding (carteira invisível)

- **Cupom/convite OPCIONAL:** o registro **não exige** cupom.
  - **Sem cupom:** usuário recebe o crédito normal da plataforma (**faucet R$20** equivalente).
  - **Com cupom:** recebe **faucet R$20 + o valor do cupom** (acrescido, on-chain via `CouponRedeemer`).
- **Confirmação dupla (obrigatória no cadastro):** **e-mail** (link/token) **E telefone** (OTP via **WhatsApp + SMS**, provedor **Zenvia/TotalVoice**). Conta só ativa após ambos verificados.
- **Idade mínima:** **18+** (responsabilidade legal plena; auto-custódia/contratos on-chain).
- **CPF:** **OPCIONAL** (anti-sybil extra; sem FLP não é obrigatório; dado sensível sob LGPD).
- **Login:** passwordless por **código de e-mail** em 2 passos (`POST /api/auth/login/request` envia o código; `POST /api/auth/login/confirm` troca o código por JWT; resposta neutra anti-enumeração). *(Passkey/WebAuthn e login social Google+Apple: roadmap — não implementados.)*
- Ao ativar: backend gera **carteira EOA managed** (mesma UX de carteira invisível — desvio formalizado no ADR-0015; Safe 4337 é roadmap) + **faucet R$20** (+ cupom se houver).
- **Anti-sybil:** 1 conta/dispositivo (fingerprint) · 5 contas/IP/dia · **e-mail único** · **telefone único**.

### 5.1 Campos do cadastro
**Obrigatórios:** nome/apelido · e-mail (verificado) · telefone (OTP WhatsApp+SMS) · CEP (→ ViaCEP) · passkey (WebAuthn) *(roadmap)* · aceite de Termos + LGPD · idade ≥18 (declaração/data) · cupom (opcional).

**Opcionais:** CPF (opcional) · avatar (**auto-gerado**: inicial+cor/DiceBear se vazio) · bio · categorias de interesse (sugestão de feed/comunidades) · foto de capa.

### 5.2 Automações (preencher pelo usuário, sem digitar)
| Automação | Entrada → Saída |
|-----------|-----------------|
| **ViaCEP** | CEP → rua, bairro, cidade, estado |
| **Geocodificação** | endereço → lat/lng (Nominatim/OSM ou Google) |
| **HTML5 geolocation** | consentimento → lat/lng preciso |
| **Comunidade default** | cidade → auto-vínculo à comunidade da cidade |
| **Device fingerprint** | navegador/dispositivo → anti-sybil (1 conta/dispositivo) |
| **Avatar automático** | nome/e-mail → inicial+cor (DiceBear) se vazio |
| **Validação de formato** | e-mail/telefone/CEP → máscara + validação instantânea |
| **Sugestão de apelido** | e-mail → apelido sugerido (editável) |

> **Campo de cadastro — benchmark e decisões travadas:** ver `MARKET_RESEARCH.md` §9 e `ADR-0013`.

### Cupom/Convite (on-chain, OPCIONAL)
- `CouponRedeemer` valida (`maxUses`, `expiry`, `usedBy`) → mint **RVM extra** na Safe (somado ao faucet).
- Admin: CRUD off-chain + deploy/revogação on-chain. Resgate **único por carteira**.
- Cupom é **incentivo de captação**, não portão de entrada.

---

## Acesso (anônimo × autenticado)

> **Gate de ação:** o revoa é **aberto para navegar, fechado para agir.**

| Quem | Pode | Não pode |
|------|------|----------|
| **Anônimo** (sem login) | **Ver** o feed, listings, comunidades públicas, perfis públicos (reputação/selos), posts públicos; **buscar/filtrar**; ver comparativo de preços | Ofertar, pedir doação/voluntariado, publicar anúncio, postar/comentar, chat, transferir RVM, avaliar |
| **Autenticado + verificado** (e-mail **E** WhatsApp/telefone confirmados) | Tudo do anônimo + **todas as ações** (ofertar, pedir, doar, publicar, postar, chat, transferir, avaliar) | — |

- **Toda ação que modifica estado** exige login + verificação dupla (e-mail + WhatsApp/telefone).
- CTA de ação para anônimo → leva ao cadastro ("entre para oferecer/pedir/doar"), preservando o contexto de volta.
- Ver detalhes de contato (telefone/endereço) de um anunciante pode exigir login (privacidade/anti-scraping).

## 6. Reputação & Ajuda Mútua

- **1–5 estrelas** por troca/doação/voluntariado finalizado (ambos se avaliam; 1× por par por interação).
- **Média** aritmética visível no perfil: "★★★★☆ (4.2 — 15 avaliações)".
- **Níveis/badges** (`Reputation.Level` no código): 🌱 Iniciante (<50 pts) → ⭐ Ajudante (<200) → 🌟 Mentor (<500) → 💎 Guardião (500+).
- **Selos de ajuda:** doador 🎁 / voluntário 🤝 (por nº de doações/voluntariados).
- **Pontos de ajuda:** contador separado (não RVM, não conversível); ranking comunitário.
- **Bônus de RVM** creditado ao doador/voluntário (`DonationReward:BonusRvm` admin-configurável).
- Moderador/admin pode remover avaliação abusiva (reputação recalculada).

---

## 7. Governança & Roles (globais)

| role | permissões |
|------|-----------|
| **user** | CRUD de listings próprios, ofertas, chat, avaliar, denunciar, transferir RVM, comunidade |
| **mod** | tudo de user + moderar (escopo comunidade ou global) + resolver denúncias + ban temp |
| **admin** | tudo de mod + categorias/cupons on-chain + ban perm + promover/rebaixar roles + dashboard + ARBITRATOR |

---

## 8. Moderação & Disputas

- **Denúncia (report):** alvo = listing | usuário | post | chat. Status `open → resolved`.
- **Auto-ocultação:** 3+ denúncias contra o mesmo conteúdo → removido do feed até revisão.
- **Disputa de escrow:** só na janela 72h; árbitro (ARBITRATOR) decide (libera/reembolsa).
- **Ban** temporário (1/7/30d, mod) ou permanente (admin). Listings desativados, login bloqueado.
- **Auditoria** on-chain (UserOps + ops autorizadas) + off-chain (logs).

---

## 9. Edge Cases & Regras Especiais

| Caso | Regra |
|------|-------|
| Doar/voluntariar (0 RVM) | Escrow/voucher reusados com valor 0; receptor não paga; doador/voluntário recebe **recompensa multi-eixo** (reputação + bônus RVM admin + pontos de ajuda) |
| Matching de doação | Doador/voluntário **escolhe** o receptor (curadoria humana, não automático) |
| Stock de produto | Decrementa **só** na liberação (não no offer). Produto sem stock → indisponível |
| Serviço sem NFT até venda | Não há token até o voucher ser mintado (mint-on-purchase) |
| Docs legados (sem `Version`) | `$or` + `$exists:false` (blueprint equivale) |
| Ownership | `SellerId`/`BuyerId`/`ProviderId` vêm do **token**, nunca do body |
| Voucher expirado | Auto-reembolso (buyer desbloqueia) + provider pode claim via árbitro |
| P2P transfer | Entre carteiras, sem taxa no MVP — *(roadmap: ainda sem endpoint)* |
| Demurrage | Não atinge saldo em escrow nem abaixo do piso (R$100 equiv) |
| Comunidade private | `join` exige `{password}` (password-gated) |

---

## 10. Glossário

| Termo | Definição |
|-------|-----------|
| **RVM** | "crédito de troca" on-chain (ERC-20). Exibição **RM$**. NÃO ativo financeiro. |
| **Safe** | Carteira do usuário (Account Abstraction, self-custody, passkey). Hoje: EOA managed pelo backend (ADR-0015); AA é roadmap. |
| **EscrowVault** | Custódia on-chain de NFT/RVM durante a troca (atomic swap). |
| **Anúncio** | "Uma coisa que você oferece" (OOUX) — product ou service, com modo e visibilidade. |
| **Atomic swap** | Troca simultânea e irrevogável de RVM↔NFT/voucher na liberação. |
| **Demurrage** | Queima periódica de saldo parado (incentiva circulação). |
| **ARBITRATOR** | Role que resolve disputas de escrow. |
| **Materialized path** | Estrutura de posts recursivos (resposta a resposta, depth 6). |
| **Haversine** | Fórmula de distância entre lat/lng (raio de feed). |

---

*Documento-fonte-de-verdade de regras. Sucessor do `trocadeira/PLANNING.md` (cenários) e `equivale/BUSINESS.md`. Atualizar a cada feature.*
