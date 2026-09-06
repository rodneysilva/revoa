# OOUX.md — Mapa Completo de Objetos ORCA (revoa.me)

> **Design objects-first (ORCA).** Os usuários pensam em **objetos**, não em telas/fluxos. Este mapa
> modela os objetos da mente do usuário ANTES da UI. Cada objeto → um **aggregate DDD**. Metodologia:
> skill `ooux` (`.kilo/skills/ooux/SKILL.md`) — processo **ORCA** (Objects→Relationships→CTAs→Attributes/Views/States).
> **Anti-padrões:** actions-first, page-first, objeto fantasma (sem CTA), atributo-objeto confusão.
>
> **Mapeamento completo** (v3.2): 32 fluxos → 26 objetos. Referências: `BUSINESS.md` · `BUSINESS_RULES.md` · `USER_FLOWS.md`.

---

## Parte 1 — Mapeamento Fluxo → Objetos (ORCA passo 1)

> Cada fluxo (UF-NN) é decomposto nos objetos que o usuário manipula. Isso garante que **nenhum objeto
> fique de fora** e que cada fluxo tenha dono(s) de aggregate claro(s).

### A. Acesso & Cadastro
| Fluxo | Objetos envolvidos |
|-------|--------------------|
| **UF-01** Navegação anônima | Anúncio, Comunidade, Post, Usuário(perfil), ReferênciaPreço (só leitura) |
| **UF-02** Cadastro sem cupom | Usuário, Verificação(e-mail), Verificação(WhatsApp), Credencial/Passkey, Carteira, Comunidade(default) |
| **UF-03** Cadastro com cupom | + Cupom, CouponRedeemer(on-chain) |
| **UF-04** Login | Credencial/Passkey (ou login social Google/Apple), Usuário |
| **UF-05** Recuperação de conta | RecuperaçãoConta (admin+timelock), Usuário |
| **UF-06** Onboarding | Usuário, Comunidade(default), Anúncio(CTA criar) |

### B. Anúncios (criar — 5 combinações kind×modo)
| Fluxo | kind | modo | Objetos |
|-------|------|------|---------|
| **UF-07** | product | trocar | Anúncio, ProductDetails, ProductNFT(mint-to-escrow), Categoria, Localização, ReferênciaPreço |
| **UF-08** | product | repassar | Anúncio, ProductDetails, ProductNFT, Categoria, Localização, ReferênciaPreço |
| **UF-09** | product | doar | Anúncio, ProductDetails, ProductNFT, Doação/Ajuda, Categoria, Localização |
| **UF-10** | service | trocar | Anúncio, ServiceDetails, Categoria, Localização, ReferênciaPreço (sem NFT até venda) |
| **UF-11** | service | voluntariar | Anúncio, ServiceDetails, Doação/Ajuda, Categoria, Localização |

### C. Transações & Ajuda
| Fluxo | Objetos |
|-------|---------|
| **UF-12** Comprar produto | Troca, EscrowVault, Carteira(buyer), ProductNFT, Avaliação |
| **UF-13** Contratar serviço | Troca, ServiceVoucher(mint-on-purchase), Carteira, Avaliação |
| **UC-14** Doar produto | Doação/Ajuda, PedidoDeAjuda, ProductNFT, Recompensa, PontosDeAjuda, Avaliação |
| **UF-15** Voluntariar serviço | Doação/Ajuda, PedidoDeAjuda, ServiceVoucher(voluntariado), Recompensa, PontosDeAjuda |
| **UF-16** Transferir RVM P2P | Transferência, Carteira(from/to) |
| **UF-17** Resgatar cupom on-chain | Cupom, CouponRedeemer, Carteira |

### D. Comunidade
| Fluxo | Objetos |
|-------|---------|
| **UF-18** Criar comunidade | Comunidade, Membership(criador) |
| **UF-19** Entrar/sair comunidade | Membership, Comunidade |
| **UF-20** Postar/responder (recursivo depth 6) | Post, Reply(=Post aninhado), Comunidade |
| **UF-21** Chat da comunidade (tempo real) | Chat(Membership), Comunidade |
| **UF-22** Denunciar conteúdo | Denúncia, (Anúncio/Post/Usuário) |

### E. Confiança & Governança
| Fluxo | Objetos |
|-------|---------|
| **UF-23** Avaliar | Avaliação, Usuário(reputação), PontosDeAjuda |
| **UF-24** Disputar escrow | Disputa, Troca, Árbitro(ARBITRATOR) |
| **UF-25** Moderar | Denúncia, Anúncio/Post/Usuário, Usuário(ban) |

### F. Carteira, Economia & Pricing
| Fluxo | Objetos |
|-------|---------|
| **UF-26** Ver carteira | Carteira, Transferência, DemurrageEntry, Recompensa |
| **UF-27** Demurrage (preview/run admin) | DemurrageEntry, ParâmetroSistema, Carteira |
| **UF-28** Ver comparativo de preço | ReferênciaPreço, Anúncio |

### G. Admin & Plataforma
| Fluxo | Objetos |
|-------|---------|
| **UF-29** Gerenciar cupons | Cupom, CouponRedeemer(on-chain) |
| **UF-30** Gerenciar parâmetros | ParâmetroSistema (bônus doação, demurrage, taxa, faucet) |
| **UF-31** Dashboard / transparência | Estatísticas (read model), revoa.org |
| **UF-32** Receber notificação | Notificação (in-app SignalR / Web Push) |

> **Ver USER_FLOWS.md** para as jornadas + máquina de estados + Gherkin de cada fluxo.

---

## Parte 2 — Catálogo de Objetos (ORCA completo)

### 1. Usuário
- **Core:** id, nome/apelido, e-mail (verificado), telefone (verificado OTP WhatsApp+SMS), localização (CEP→ViaCEP, lat/lng, bairro, cidade), role (`user`/`mod`/`admin`), idade≥18.
- **Metadata:** avatar (auto: inicial+cor/DiceBear), bio, CPF (opcional), login social (Google/Apple), reputação (média+badges), data cadastro, status.
- **CTAs:** cadastrar (cupom opcional), login (passkey/social), editar perfil, listar, comprar, transferir, doar/voluntariar, avaliar, denunciar, entrar/sair comunidade.
- **Relacionamentos:** → Carteira(1:1), Anúncio(1:N), Comunidade(N:M via Membership), Troca(1:N), Avaliação(1:N), PedidoDeAjuda(1:N).
- **Views:** perfil público/próprio, card, admin.
- **States:** `active | banned(temp/perm) | inactive`.
- **Aggregate:** `Identity.User`. **Ranking:** 1.

### 2. Verificação (OTP e-mail / WhatsApp)
- **Core:** alvo (e-mail ou telefone), código OTP, canal (link/WhatsApp/SMS), expiry, tentativas.
- **CTAs:** enviar código, reenviar, validar (digitar código).
- **States:** `pendente → validado | expirado`.
- **Views:** modal/passo de cadastro, reenvio.
- **Aggregate:** `Identity.Verification` (efêmero; ttl curto). **Ranking:** 2.

### 3. Credencial / Passkey (WebAuthn)
- **Core:** id, usuário, publicKey (P-256), dispositivo/label, createdAt.
- **CTAs:** criar (cadastro), ver dispositivos, revogar, adicionar nova.
- **States:** `ativa | revogada`.
- **Views:** segurança do perfil, gerenciar dispositivos.
- **Aggregate:** `Identity.Credential`. **Ranking:** 2.

### 4. Carteira
- **Core:** endereço (Safe), saldoRvm disponível, saldoBloqueado (escrow).
- **CTAs:** ver saldo, receber (faucet/cupom/venda/P2P), transferir (P2P), bloquear (escrow).
- **Relacionamentos:** → Usuário(1:1), Transferência(1:N), Troca(1:N).
- **Views:** header (RM$), página carteira, detalhe transação.
- **States:** implícitos (disponível/bloqueado).
- **Aggregate:** `Account.Wallet` (read model do Indexer). **Ranking:** 1.

### 5. Recuperação de Conta
- **Core:** usuário, motivo, árbitro, timelock (início/expira), decisão.
- **CTAs:** solicitar, aprovar (admin, após timelock), executar (transferir custódia).
- **States:** `solicitada → em_timelock → aprovada → executada | negada`.
- **Views:** admin (árbitro), solicitação do usuário.
- **Aggregate:** `Identity.AccountRecovery`. **Ranking:** 3 (privado: admin+timelock; público: guardians).

### 6. Cupom / Convite (on-chain)
- **Core:** code(hash), amount, maxUses, expiry, usedBy[].
- **CTAs:** criar (admin), resgatar (mint RVM extra), revogar.
- **Relacionamentos:** → Carteira(mint), Usuário(usado no cadastro).
- **States:** `active → exhausted | expired | revoked`.
- **Views:** admin (CRUD), onboarding (campo opcional).
- **Aggregate:** `Token.Coupon` (contrato `CouponRedeemer`). **Ranking:** 2.

### 7. Anúncio (kind)
- **Core:** id, kind(`product`/`service`), modo(`trocar`/`repassar`/`doar`/`voluntariar`), título, descrição, imagens(MinIO), preçoRvm, vendedor(embed), localização, categoria, visibilidade.
- **Details (VO por kind):** `ProductDetails`(condition, stock, nftTokenId) · `ServiceDetails`(unitType, duration, voucherExpiry).
- **Comparativo:** refBRL, medianaRvm, sugestão.
- **CTAs:** publicar, editar, excluir, comprar/contratar, doar/pedir, denunciar, favoritar, compartilhar.
- **Relacionamentos:** → Usuário(vendedor), Categoria, Comunidade(opt), ProductNFT/ServiceVoucher, Troca/Doação, ReferênciaPreço.
- **Views:** card no feed (raio/kind/categoria/comunidade), detalhe, gerenciar.
- **States:** `rascunho → ativo → em_andamento → concluído | cancelado`.
- **Aggregate:** `Catalog.Listing`. **Ranking:** 1.

### 8. Categoria
- **Core:** id, nome, slug, descrição.
- **CTAs:** criar/editar/excluir (admin), filtrar feed.
- **States:** `active | inactive` (soft-delete).
- **Views:** filtros do feed, admin.
- **Aggregate:** `Catalog.Category`. **Ranking:** 2.

### 9. ProductNFT (token ERC-721)
- **Core:** tokenId, contrato, anúncioId, owner (Vault→buyer), metadataURI (MinIO).
- **CTAs:** mint-to-escrow (ao listar), transfer (na liberação/cancelamento).
- **States:** `inVault → owned | devolvido`.
- **Aggregate:** `Exchange` (custódia on-chain; Indexer projeta). **Ranking:** 2.

### 10. ServiceVoucher (token ERC-1155)
- **Core:** tokenId, anúncioId, owner, expiry(30d), tipo(`compra`/`voluntariado`).
- **CTAs:** mint-on-purchase/voluntariar, redeem, confirm, burn, claim(árbitro se expirar).
- **States:** `emitido → resgatado → queimado | expirado`.
- **Aggregate:** `Exchange`. **Ranking:** 2.

### 11. Troca (escrow)
- **Core:** id, anúncio, buyer, seller, valorRvm, taxa(2%→Fundo), nft/voucher, createdAt.
- **CTAs:** ofertar, pagar(fund), liberar (confirmação cooperativa), disputar, cancelar.
- **States:** `ofertada → financiada → liberada|disputada` (liberação cooperativa; auto após 72h sem ação); `cancelada → reembolsada`.
- **Views:** tracker de troca, histórico, admin (árbitro).
- **Aggregate:** `Exchange.Trade` (ACID no finish). **Ranking:** 1.

### 12. Doação / Ajuda
- **Core:** anúncio(modo doar/voluntariar), doador, receptor(escolhido), voucher/NFT, recompensas aplicadas, createdAt.
- **CTAs:** anunciar(0 RVM), pedir(fila), escolher receptor(curadoria), aceitar, liberar, cancelar.
- **Relacionamentos:** → Anúncio, Usuário(doador+receptor), PedidoDeAjuda, Recompensa, PontosDeAjuda.
- **States:** `anunciada → pedida (Open) → combinada (Selected) → liberada → recompensada | cancelada | retirada (Withdrawn)`.
- **Views:** feed (badge Doar/Voluntariar), detalhe, "minhas doações", perfil (selos).
- **Aggregate:** `Exchange.Donation` (discriminado por modo; reusa escrow/voucher valor 0). **Ranking:** 1.

### 13. Pedido de Ajuda (Ask)
- **Core:** autor (verificado), anúncio-alvo (ou pedido livre), mensagem, createdAt.
- **CTAs:** pedir, editar, retirar, ser escolhido, agradecer.
- **States:** `open → selected | withdrawn | closed`.
- **Views:** fila de pedidos, "pedidos abertos" da comunidade.
- **Aggregate:** `Catalog.HelpRequest`. **Ranking:** 2.

### 14. Transferência (P2P)
- **Core:** id, from(Safe), to(Safe), valorRvm, txHash, createdAt.
- **CTAs:** enviar, receber.
- **States:** `pendente → confirmada`.
- **Views:** carteira (histórico), notificação.
- **Aggregate:** `Account.Transfer` (UserOp on-chain). **Ranking:** 2.

### 15. Avaliação
- **Core:** id, trade/doação, reviewer, reviewed, nota(1–5), comentário.
- **CTAs:** criar (ao finalizar), remover (mod).
- **States:** `criada → (removida?)`.
- **Views:** perfil (reputação), detalhe da troca/doação.
- **Aggregate:** `Reputation.Review`. **Ranking:** 3.

### 16. Pontos de Ajuda
- **Core:** usuário, saldo, histórico (por doação/voluntariado), selos desbloqueados.
- **CTAs:** acumular, ver ranking, ver selos.
- **States:** implícitos (saldo + marcos).
- **Views:** perfil (selos + pontos), ranking comunitário.
- **Aggregate:** `Reputation.HelpPoints` (não-RVM). **Ranking:** 3.

### 17. Comunidade
- **Core:** id, nome, tipo(default/user), eixo(geo/interesse/causa), localização, visibilidade(open/private), criador.
- **CTAs:** criar, editar/excluir (criador/admin), nomear moderador, entrar/sair.
- **Relacionamentos:** → Usuário(N:M via Membership), Post(1:N), Chat(1:1), Anúncio(N visibilidade).
- **States:** `active | archived`.
- **Views:** feed da comunidade, lista, card, admin.
- **Aggregate:** `Community.Community`. **Ranking:** 2.

### 18. Membership
- **Core:** usuário, comunidade, papel(`membro`/`moderador`/`criador`), joinedAt.
- **CTAs:** entrar, sair, promover (criador), ser moderador.
- **States:** `ativa | bloqueada`.
- **Views:** membros da comunidade, perfil.
- **Aggregate:** `Community.Membership`. **Ranking:** 2.

### 19. Post (recursivo) / Reply
- **Core:** id, comunidade, autor(embed), conteúdo, path(materialized), depth(≤6), parent.
- **CTAs:** postar, responder (até depth 6), editar, excluir, denunciar, ocultar (mod, cascata).
- **States:** `visível → oculto` (cascata a descendentes).
- **Views:** feed da comunidade, thread, moderação.
- **Aggregate:** `Community.Post`. **Ranking:** 2.

### 20. Chat (Membership)
- **Core:** comunidade, mensagens[], autor, conteúdo, createdAt.
- **CTAs:** enviar, reagir, denunciar, ocultar (mod).
- **States:** mensagem viva → **expira em 90 dias**.
- **Views:** painel de chat (SignalR, tempo real).
- **Aggregate:** `Community.Chat`. **Ranking:** 2.

### 21. Notificação
- **Core:** id, usuário, tipo (escrow/oferta/transferência/post/chat/preço/doação), payload, lida.
- **CTAs:** marcar lida, dismiss, abrir (deep link).
- **States:** `unread → read`.
- **Views:** badge header, lista, push (PWA), in-app (SignalR).
- **Aggregate:** `Notifications.Notification`. **Ranking:** 3.

### 22. Denúncia
- **Core:** denunciante, alvo (anúncio/post/usuário/chat), motivo, descrição, status.
- **CTAs:** denunciar, resolver (mod/admin), auto-ocultar (3+ denúncias).
- **States:** `open → resolved`.
- **Views:** fila de moderação, detalhe.
- **Aggregate:** `Moderation.Report`. **Ranking:** 3.

### 23. Disputa
- **Core:** troca, motivo, evidências, árbitro, decisão, timestamps.
- **CTAs:** abrir (janela 72h), analisar, decidir (libera/reembolsa).
- **States:** `open → resolved`.
- **Views:** painel do árbitro, histórico da troca.
- **Aggregate:** `Moderation.Dispute`. **Ranking:** 3.

### 24. Referência de Preço
- **Core:** categoria, refBRL (ML+seed+comunidade), medianaRvm, sugestão, origem, dataRefresh.
- **CTAs:** recalcular (job semanal/trimestral), ver histórico (transparência).
- **States:** implícitos (snapshot por data).
- **Views:** comparativo no anúncio, página de transparência.
- **Aggregate:** `PricingIntelligence.PriceReference`. **Ranking:** 3.

### 25. Parâmetro de Sistema (admin-configurável)
- **Core:** chave, valor (faucet R$20, taxa 2%, demurrage 0,5%/piso R$100, `DonationReward:BonusRvm` por kind/modo, validez voucher 30d, janela disputa 72h, retenção chat 90d).
- **CTAs:** editar (admin), versionar (auditoria), ver histórico.
- **States:** implícitos (snapshot versionado; IPCA reajusta trimestralmente).
- **Views:** painel admin (parâmetros).
- **Aggregate:** `Token.SystemParameter` (ou `Abstractions.Settings`). **Ranking:** 2.

### 26. Demurrage (Aplicação / Ledger)
- **Core:** período, taxa aplicada, base (IPCA-reajustada), entradas por usuário, total queimado.
- **CTAs:** preview (admin), run (keeper/scheduler), ver ledger.
- **States:** `agendado → aplicado`.
- **Views:** admin (preview/run), histórico da carteira (queima).
- **Aggregate:** `Token.Demurrage` (`DemurrageEntry` ledger; keeper restart-safe/idempotente). **Ranking:** 3.

---

## Parte 3 — Matriz de Relacionamentos (cross-check)

| De | Para | Cardinalidade | Via |
|----|------|---------------|-----|
| Usuário | Carteira | 1:1 | onboarding |
| Usuário | Credencial/Passkey | 1:N | segurança |
| Usuário | Verificação | 1:N | cadastro (e-mail + WhatsApp) |
| Usuário | Anúncio | 1:N | vendedor (embed nome) |
| Usuário | Comunidade | N:M | Membership |
| Usuário | Troca | 1:N | buyer/seller |
| Usuário | Doação/Ajuda | 2:1 | doador + receptor |
| Usuário | Pedido de Ajuda | 1:N | solicita doação/voluntariado |
| Usuário | Avaliação | 1:N | reviewer/reviewed |
| Usuário | Pontos de Ajuda | 1:1 | acumula |
| Anúncio | ProductNFT | 1:1 (product) | tokeniza ao listar |
| Anúncio | ServiceVoucher | 1:N (service) | tokeniza ao comprar/voluntariar |
| Anúncio | Categoria | N:1 | category |
| Anúncio | Comunidade | N:1 (opt) | visibility |
| Anúncio | ReferênciaPreço | N:1 | comparativo |
| Anúncio | Troca/Doação | 1:N | origem |
| Troca | Disputa | 1:1 (opt) | só se disputada |
| Troca/Doação | Avaliação | 1:N (≤2) | ao finalizar |
| Doação/Ajuda | Pedido de Ajuda | N:1 | receptor manifesta |
| Cupom/Convite | Carteira | 1:N | mint RVM (resgate) |
| Cupom/Convite | Usuário | 1:N | usado no cadastro (opcional) |
| Transferência | Carteira | 2:1 | from→to (P2P) |
| Comunidade | Membership | 1:N | membros |
| Comunidade | Post | 1:N | feed |
| Comunidade | Chat | 1:1 | chat geral |
| Post | Post (Reply) | 1:N | materialized path depth 6 |
| Denúncia | (Anúncio/Post/Usuário/Chat) | N:1 | alvo |
| ParâmetroSistema | Demurrage/Cupom/Doação | 1:N | configura |
| **Anônimo** | Anúncio/Comunidade/Post | N:M | **só visualiza** (gate de ação) |

---

## Parte 4 — Ranking Forçado (prioridade/fase)

| Rank | Objetos | Fase |
|------|---------|------|
| 1 | Usuário, Carteira, Anúncio, Cupom, Troca, Doação/Ajuda | Fase 1–2 (core) |
| 2 | Verificação, Credencial, ProductNFT, ServiceVoucher, Categoria, Pedido de Ajuda, Comunidade, Membership, Post, Chat, Transferência, ParâmetroSistema | Fase 1–2 |
| 3 | RecuperaçãoConta, Avaliação, Pontos de Ajuda, Notificação, Denúncia, Disputa, ReferênciaPreço, Demurrage | Fase 3 (confiança/inteligência) |

> **Regra YAGNI:** não modelar objetos de fases futuras além do necessário. Cada novo fluxo começa
> aqui (Parte 1), extrai objetos, e só vira aggregate/código o que tem CTA/estado reais.

---

*Mapa-fonte-de-verdade de objetos (ORCA). Cada objeto é a semente de um aggregate DDD, respeitando isolamento de módulo. Atualizar a cada fluxo novo.*
