# OOUX.md — Mapa de Objetos ORCA do revoa.me

> **Design objects-first.** Os usuários pensam em **objetos** (pessoas, coisas, lugares, conceitos),
> não em telas/fluxos. Este mapa modela os objetos da mente do usuário ANTES de qualquer UI.
> Cada objeto → um **aggregate DDD** (alinhamento arquitetural). Metodologia: skill `ooux` (`.kilo/skills/ooux/SKILL.md`).
>
> Referências: Plano §9a · `BUSINESS.md` · `BUSINESS_RULES.md` · `ARCHITECTURE.md`.

---

## Visão Geral — 19 Objetos

> **Ajuda mútua (doação/voluntariado) é primeira classe.** Objetos novos nesta rodada:
> **Doação/Ajuda** (interação de ajuda, modo doar/voluntariar), **Pedido de Ajuda (Ask)** (manifestação
> de interesse do receptor) e **Pontos de Ajuda** (contador não-RVM de boas ações).

```
                              ┌──────────┐
                              │ Usuário  │──────────┐
                              └────┬─────┘          │
                  ┌────────────────┼───────────┐    │ owns
            owns  │                │ member of │    ▼
                  ▼                ▼           │ ┌──────────┐
              ┌────────┐      ┌────────┐       │ │ Carteira │── RVM
              │Carteira│      │Comunid.│──────┘ └────┬─────┘
              └───┬────┘      └───┬────┘              │ P2P
                  │               │ posts             ▼
           offers│          ┌─────▼─────┐       ┌──────────┐
                  ▼          │   Post    │       │Transfere.│
              ┌────────┐     │(recursivo)│       └──────────┘
              │Anúncio │─────┴───────────┘
              │(kind)  │ tokeniza
              └───┬────┘
        ┌─────────┼──────────┐
        ▼         ▼          ▼
   ┌─────────┐┌────────┐┌────────┐
   │ProductNF││Service ││ Troca  │── disputa ──► Disputa
   │T        ││Voucher ││(escrow)│
   └─────────┘└────────┘└───┬────┘
                            │ finaliza
                            ▼
                       ┌─────────┐  resgata   ┌────────────┐
                       │Avaliação│            │Cupom/Convite│── mint RVM
                       └─────────┘            └────────────┘
  Categoria ─(organiza)─► Anúncio     Notificação ─(avisa)─► Usuário
  Ref. Preço ─(orienta)─► Anúncio     Chat(Membership) ─(dentro de)─► Comunidade
```

### Matriz de Relacionamentos (cardinalidade)
| De | Para | Cardinalidade | Via |
|----|------|---------------|-----|
| Usuário | Carteira | 1:1 | (criação onboarding) |
| Usuário | Anúncio | 1:N | seller (embed name) |
| Usuário | Comunidade | N:M | Membership |
| Usuário | Troca | 1:N | buyer/seller |
| Usuário | Avaliação | 1:N | reviewer/reviewed |
| Anúncio | ProductNFT | 1:1 (product) | tokeniza ao listar |
| Anúncio | ServiceVoucher | 1:N (service) | tokeniza ao comprar |
| Anúncio | Categoria | N:1 | category |
| Anúncio | Comunidade | N:1 (opt) | visibility |
| Anúncio | Ref. Preço | N:1 | comparativo |
| Anúncio | Troca | 1:N | origem |
| Troca | Disputa | 1:1 (opt) | só se disputada |
| Troca | Avaliação | 1:N (≤2) | ao finalizar |
| Comunidade | Post | 1:N | feed |
| Comunidade | Chat(Membership) | 1:1 | chat geral |
| Post | Post (Reply) | 1:N | materialized path depth 6 |
| Cupom/Convite | Usuário | 1:N | usado no cadastro (maxUses) |
| Transferência | Carteira | 2:1 | from→to (P2P) |
| Doação/Ajuda | Anúncio | 1:N | anúncio modo doar/voluntariar (valor 0) |
| Pedido de Ajuda | Doação/Ajuda | N:1 | receptor manifesta interesse (fila) |
| Doação/Ajuda | Usuário | 2:1 | doador + receptor |
| Pontos de Ajuda | Usuário | 1:1 | acumula por doação/voluntariado |

---

## Objetos (detalhe ORCA)

### 1. Usuário
- **Core:** id, nome, email, avatar, localização (lat/lng, bairro, cidade), role (`user`/`mod`/`admin`).
- **Metadata:** bio, reputação (média/badges), data cadastro, status (`active`/`banned`).
- **CTAs:** cadastrar (cupom-gated), editar perfil, criar passkey, listar, comprar, transferir, avaliar, denunciar, entrar/sair comunidade.
- **States:** `active` | `banned(temp/perm)` | `inactive`.
- **Views:** perfil público, perfil próprio, card no feed/chat, admin.
- **Aggregate DDD:** `Identity.User`.

### 2. Carteira
- **Core:** endereço (Safe), saldoRvm (on-chain, via Indexer), saldoBloqueado (escrow).
- **Metadata:** createdAt, tipo (user/treasury).
- **CTAs:** ver saldo, receber (faucet/cupom/venda/P2P), transferir (P2P), bloquear (escrow).
- **States:** implícitos pelo saldo (disponível/bloqueado).
- **Views:** header (saldo RM$), página carteira, detalhe transação.
- **Aggregate DDD:** `Account.Wallet` (read model do Indexer).
- **OOUX→UX:** "carteira invisível" — o usuário vê saldo, não chaves/seed.

### 3. Anúncio (kind)
- **Core:** id, kind (`product`/`service`), modo (`trocar`/`repassar`/`doar`/`voluntariar`), título, descrição, imagens, preçoRvm, vendedor (embed nome/avatar), localização, categoria, visibilidade.
- **Details (VO por kind):** `ProductDetails`(condition, stock, nftTokenId) · `ServiceDetails`(unitType, duration, voucherExpiry).
- **Comparativo:** refBRL, medianaRvm, sugestão.
- **CTAs:** publicar, editar, excluir, comprar/contratar, denunciar, favoritar, compartilhar.
- **States:** `draft → active → in_progress → sold|cancelled` (produtos); serviços similares.
- **Views:** card no feed (por raio/kind/categoria/comunidade), detalhe, gerenciar.
- **Aggregate DDD:** `Catalog.Listing`.
- **OOUX→UX:** "uma coisa que você oferece" — formulário dinâmico por kind.

### 4. ProductNFT (token)
- **Core:** tokenId, contrato, anúncioId, owner (Vault durante anúncio; comprador após release), metadataURI (MinIO).
- **Metadata:** atributos (condition, imagens).
- **CTAs:** mint-to-escrow (ao listar), transfer (na liberação do swap).
- **States:** `inVault → owned` (após atomic swap).
- **Views:** badge no anúncio, histórico on-chain.
- **Aggregate DDD:** `Exchange` (custódia on-chain; Indexer projeta).

### 5. ServiceVoucher (token)
- **Core:** tokenId(1155), anúncioId, owner (comprador), expiry (30d).
- **Metadata:** unitType, duração.
- **CTAs:** mint-on-purchase, redeem (usar), confirm, burn (na liberação), claim (árbitro se expirar).
- **States:** `issued → redeemed → burned` | `expired`.
- **Views:** carteira do comprador (meus vouchers), detalhe.
- **Aggregate DDD:** `Exchange` (custódia on-chain).

### 6. Troca (escrow)
- **Core:** id, anúncio, buyer, seller, valorRvm, taxa(2%), voucher/nft, createdAt.
- **States:** `offered → funded → delivered → [72h] → released|disputed` (produto); serviço análogo + `redeemed`; `cancelled → refunded`.
- **CTAs:** ofertar, pagar(fund), marcar entregue, confirmar, disputar, liberar, cancelar.
- **Views:** tracker de troca, histórico, admin (árbitro).
- **Aggregate DDD:** `Exchange.Trade` (transação ACID no finish).

### 7. Transferência (P2P)
- **Core:** id, from(Safe), to(Safe), valorRvm, txHash, createdAt.
- **CTAs:** enviar, receber.
- **States:** `pending → confirmed`.
- **Views:** carteira (histórico), notificação.
- **Aggregate DDD:** `Account.Transfer` (UserOp on-chain).

### 8. Avaliação
- **Core:** id, trade, reviewer, reviewed, nota(1–5), comentário.
- **CTAs:** criar (ao finalizar), remover (mod).
- **States:** `created → (removida?)`.
- **Views:** perfil (reputação), detalhe da troca.
- **Aggregate DDD:** `Reputation.Review`.

### 9. Cupom / Convite (on-chain)
- **Core:** code(hash), amount, maxUses, expiry, usedBy[].
- **CTAs:** criar (admin), resgatar (mint RVM), revogar.
- **States:** `active → exhausted | expired | revoked`.
- **Views:** admin (CRUD cupons), onboarding (campo cupom).
- **Aggregate DDD:** `Token.Coupon` (contrato `CouponRedeemer`).

### 10. Categoria
- **Core:** id, nome, slug, descrição.
- **States:** `active | inactive` (soft-delete).
- **CTAs:** criar/editar/excluir (admin), filtrar feed.
- **Views:** filtros do feed, admin.
- **Aggregate DDD:** `Catalog.Category`.

### 11. Referência de Preço
- **Core:** categoria, refBRL (ML+seed+comunidade), medianaRvm, sugestão, origem, dataRefresh.
- **Metadata:** fonte detalhada (ML/seed/comunidade/IPCA).
- **CTAs:** recalcular (job semanal/trimestral), ver histórico (transparência).
- **States:** implícitos (snapshot por data).
- **Views:** comparativo no anúncio, página de transparência.
- **Aggregate DDD:** `PricingIntelligence.PriceReference`.

### 12. Notificação
- **Core:** id, usuário, tipo (escrow/oferta/transferência/post/chat/preço), payload, lida.
- **CTAs:** marcar lida, dismiss, abrir (deep link).
- **States:** `unread → read`.
- **Views:** badge header, lista, push (PWA), in-app (SignalR).
- **Aggregate DDD:** `Notifications.Notification`.

### 13. Comunidade
- **Core:** id, nome, tipo (default/user), eixo (geo/interesse/causa), localização, visibilidade (open/private), criador.
- **Metadata:** descrição, regras, contagem membros.
- **CTAs:** criar, editar/excluir (criador/admin), entrar/sair, nomear moderador, postar, anunciar.
- **States:** `active | archived`.
- **Views:** feed da comunidade, lista, card, admin.
- **Aggregate DDD:** `Community.Community`.

### 14. Post (recursivo)
- **Core:** id, comunidade, autor (embed), conteúdo, path (materialized), depth (≤6), parent.
- **Metadata:** anexos, reações, createdAt, hidden.
- **CTAs:** postar, responder (até depth 6), editar, excluir, denunciar, ocultar (mod).
- **States:** `visible → hidden` (cascata a descendentes).
- **Views:** feed da comunidade, thread, moderação.
- **Aggregate DDD:** `Community.Post`.

### 15. Reply (= Post aninhado)
> Mesmo aggregate que Post. Diferenciação OOUX apenas para UX (indentação, profundidade).
> **Limite depth 6** — além disso, "criar nova conversa" é sugerido.

### 16. Chat (Membership)
- **Core:** comunidade, mensagens[], autor, conteúdo, createdAt.
- **CTAs:** enviar, reagir, denunciar, ocultar (mod).
- **States:** mensagem viva → **expira em 90 dias**.
- **Views:** painel de chat da comunidade (SignalR, tempo real).
- **Aggregate DDD:** `Community.Chat` (retenção 90d, moderado como posts).

### 17. Disputa
- **Core:** troca, motivo, evidências, árbitro, decisão, timestamps.
- **CTAs:** abrir (janela 72h), analisar (árbitro), decidir (libera/reembolsa).
- **States:** `open → resolved`.
- **Views:** painel do árbitro, histórico da troca.
- **Aggregate DDD:** `Moderation.Dispute`.

### 18. Doação / Ajuda (produto de primeira classe)
> Especialização da interação quando `modo ∈ {doar, voluntariar}`. **Reusa o EscrowVault/voucher com
> valor 0** (sem RVM do receptor). O doador/voluntário recebe **recompensa multi-eixo** (reputação +
> bônus RVM admin-configurável + pontos de ajuda). NÃO confundir com Troca (que movimenta RVM).

- **Core:** anúncio (modo doar/voluntariar), doador, receptor (escolhido), voucher/NFT, recompensas aplicadas, createdAt.
- **CTAs:** anunciar (doar/voluntariar, 0 RVM), pedir (receptor manifesta), **escolher receptor** (doador curador), aceitar, entregar, confirmar, cancelar.
- **States:** `announced → requested → matched(escolhido) → delivered → confirmed → rewarded` | `cancelled`.
- **Views:** feed (badge Doar/Voluntariar), detalhe, "minhas doações", perfil (selos).
- **Aggregate DDD:** `Exchange.Donation` (compartilha agregado com Trade, discriminado por modo).

### 19. Pedido de Ajuda (Ask)
> O "pedir" (Buy Nothing) é tão importante quanto "oferecer". Todo receptor manifesta interesse numa
> Doação/Ajuda ou cria um **Pedido** aberto ("preciso de X" / "preciso de ajuda com Y").

- **Core:** autor, anúncio-alvo (ou pedido livre), mensagem, createdAt.
- **CTAs:** pedir, editar, retirar pedido, ser escolhido, agradecer (gratidão).
- **States:** `open → selected | withdrawn | closed`.
- **Views:** fila de pedidos (no anúncio de doação), "pedidos abertos" da comunidade.
- **Aggregate DDD:** `Catalog.HelpRequest` (ou `Community.Request`).

### 20. Pontos de Ajuda
- **Core:** usuário, saldo de pontos, histórico (por doação/voluntariado), ranking comunitário.
- **Metadata:** selos desbloqueados (🎁 doador, 🤝 voluntário, por marcos).
- **CTAs:** acumular (ao recompensar doação), ver ranking, ver selos.
- **States:** implícitos (saldo crescente + marcos).
- **Views:** perfil (selos + pontos), ranking da comunidade.
- **Aggregate DDD:** `Reputation.HelpPoints` (não-RVM, não conversível).

---

## Ranking Forçado (prioridade de implementação por fase)

| Prioridade | Objeto | Fase |
|-----------|--------|------|
| 1 | Usuário, Carteira, Anúncio, Cupom/Convite | Fase 1 (foundation) |
| 2 | ProductNFT, ServiceVoucher, Troca, Categoria | Fase 2 (MVP trocas) |
| 3 | Comunidade, Post, Reply, Chat, Notificação, **Doação/Ajuda, Pedido de Ajuda, Pontos de Ajuda** | Fase 2 (comunidades + ajuda mútua) |
| 4 | Avaliação, Transferência, Ref. Preço, Disputa | Fase 3 (confiança/inteligência) |

> **Regra YAGNI:** não modele objetos de fases futuras além do necessário para o contexto atual.
> Cada novo objeto começa aqui, com CTA/state reais, antes de virar aggregate/código.

---

*Mapa-fonte-de-verdade de objetos. Atualizar a cada feature (skill `ooux`, passo ORCA). Cada objeto é a semente de um aggregate DDD respeitando isolamento de módulo.*
