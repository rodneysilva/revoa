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
| modo | kind | preço RVM | efeito |
|------|------|-----------|--------|
| **trocar** | product · service | valor justo (RVM) | mercado RVM |
| **repassar** | product | RVM baixo | acessibilidade |
| **doar** | product | **0 RVM** | ajuda mútua + **bônus reputacional** |
| **voluntariar** | service | **0 RVM** | ajuda mútua + **bônus reputacional** |

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
  → Offer  (comprador manifesta interesse)
  → Funded (comprador paga RVM → buyer.Block(total); NFT segue no Vault)
  → Delivered (vendedor marca entregue / acordam logística hiperlocal)
  → [janela 72h, block.timestamp]
       ├─ Released  → atomic swap: seller.Credit(total − 2%) + treasury.Credit(2%) + NFT→buyer
       └─ Disputed → árbitro (ARBITRATOR) decide (libera/reembolsa)
  Cancelled (antes de Delivered) → Refunded: buyer.Unblock(total) + NFT→seller
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
- **Fundos bloqueados** (`Block`) ao pagar; **liberados/creditados** só na finalização (`FinishTransactionAsync`, dentro de transação ACID).
- **Taxa 2%** do vendedor, creditada à Tesouraria na liberação.
- **Janela 72h** medida em `block.timestamp` (on-chain, imutável).
- **ARBITRATOR** (admin/Safe plataforma) é a única role que resolve disputa.
- **Validade voucher 30d** → auto-reembolso + claim do provider via árbitro.

---

## 3. Os 3 Fluxos Econômicos
1. **Moeda → Anúncio:** comprar produto/serviço com RVM (atomic swap).
2. **Anúncio → Moeda:** oferecer e receber RVM (ao liberar).
3. **Moeda → Conta:** transferência **P2P** de RVM entre carteiras (self-custody, on-chain).

> P2P é entre Safes (UserOp de `transfer`). Sem taxa de transferência no MVP.

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

- **Invite/cupom-gated:** sem cupom/convite válido (on-chain, `maxUses`/`expiry`), não cria conta.
- **Email** verificado + **passkey (WebAuthn)** — **sem seed phrase**.
- Ao criar: backend gera **Safe** (4337 + módulo + signer webauthn-solidity) + **faucet R$20**.
- **Anti-sybil:** 1 conta/dispositivo · 5 contas/IP/dia · email único.

### Cupom = Convite (unificado, ON-CHAIN)
- `CouponRedeemer` valida (`maxUses`, `expiry`, `usedBy`) → mint RVM na Safe.
- Admin: CRUD off-chain + deploy/revogação on-chain.
- Resgate **único por carteira**.

---

## 6. Reputação

- **1–5 estrelas** por troca finalizada (ambos se avaliam; 1× por par por troca).
- **Média** aritmética visível no perfil: "★★★★☆ (4.2 — 15 avaliações)".
- **Níveis/badges:** 🌱 Iniciante → ⭐ Trocador → 🌟 Trocador Pro → 💎 Lenda.
- **Bônus reputacional** em doar/voluntariar (incentivo à ajuda mútua).
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
| Doar/voluntariar (0 RVM) | Sem escrow financeiro; transfer direta do NFT/voucher + bônus reputacional |
| Stock de produto | Decrementa **só** na liberação (não no offer). Produto sem stock → indisponível |
| Serviço sem NFT até venda | Não há token até o voucher ser mintado (mint-on-purchase) |
| Docs legados (sem `Version`) | `$or` + `$exists:false` (blueprint equivale) |
| Ownership | `SellerId`/`BuyerId`/`ProviderId` vêm do **token**, nunca do body |
| Voucher expirado | Auto-reembolso (buyer desbloqueia) + provider pode claim via árbitro |
| P2P transfer | Entre Safes, sem taxa no MVP |
| Demurrage | Não atinge saldo em escrow nem abaixo do piso (R$100 equiv) |
| Comunidade private | `join` exige `{password}` (password-gated) |

---

## 10. Glossário

| Termo | Definição |
|-------|-----------|
| **RVM** | "crédito de troca" on-chain (ERC-20). Exibição **RM$**. NÃO ativo financeiro. |
| **Safe** | Carteira do usuário (Account Abstraction, self-custody, passkey). |
| **EscrowVault** | Custódia on-chain de NFT/RVM durante a troca (atomic swap). |
| **Anúncio** | "Uma coisa que você oferece" (OOUX) — product ou service, com modo e visibilidade. |
| **Atomic swap** | Troca simultânea e irrevogável de RVM↔NFT/voucher na liberação. |
| **Demurrage** | Queima periódica de saldo parado (incentiva circulação). |
| **ARBITRATOR** | Role que resolve disputas de escrow. |
| **Materialized path** | Estrutura de posts recursivos (resposta a resposta, depth 6). |
| **Haversine** | Fórmula de distância entre lat/lng (raio de feed). |

---

*Documento-fonte-de-verdade de regras. Sucessor do `trocadeira/PLANNING.md` (cenários) e `equivale/BUSINESS.md`. Atualizar a cada feature.*
