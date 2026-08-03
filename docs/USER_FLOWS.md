# USER_FLOWS.md — Fluxos do Usuário (revoa.me)

> **Jornadas + máquinas de estado + user stories (Gherkin)** para **todos os fluxos** do produto.
> Numeração **UF-NN** alinhada ao mapa de objetos (`OOUX.md` Parte 1). Cada fluxo → objetos → aggregates.
> **Sem fins lucrativos** · moeda social RVM · auto-custódia · acesso "aberto p/ navegar, fechado p/ agir".

Relacionados: `BUSINESS.md` · `BUSINESS_RULES.md` · `OOUX.md` · `ARCHITECTURE.md`.

---

## Índice
**A. Acesso & Cadastro**
- [UF-01 Navegação anônima](#uf-01--navegação-anônima)
- [UF-02 Cadastro sem cupom](#uf-02--cadastro-sem-cupom)
- [UF-03 Cadastro com cupom](#uf-03--cadastro-com-cupom)
- [UF-04 Login](#uf-04--login) · [UF-05 Recuperação de conta](#uf-05--recuperação-de-conta) · [UF-06 Onboarding](#uf-06--onboarding)

**B. Anúncios (5 combinações kind×modo)**
- [UF-07 Produto — trocar](#uf-07--produto-trocar) · [UF-08 Produto — repassar](#uf-08--produto-repassar) · [UF-09 Produto — doar](#uf-09--produto-doar)
- [UF-10 Serviço — trocar](#uf-10--serviço-trocar) · [UF-11 Serviço — voluntariar](#uf-11--serviço-voluntariar)

**C. Transações & Ajuda**
- [UF-12 Comprar produto](#uf-12--comprar-produto) · [UF-13 Contratar serviço](#uf-13--contratar-serviço) · [UF-14 Doar produto](#uf-14--doar-produto) · [UF-15 Voluntariar serviço](#uf-15--voluntariar-serviço) · [UF-16 Transferir RVM P2P](#uf-16--transferir-rvm-p2p) · [UF-17 Resgatar cupom on-chain](#uf-17--resgatar-cupom-on-chain)

**D. Comunidade** — UF-18 a UF-22 · **E. Confiança & Governança** — UF-23 a UF-25 · **F. Carteira/Economia/Pricing** — UF-26 a UF-28 · **G. Admin & Plataforma** — UF-29 a UF-32

---

# A. Acesso & Cadastro

## UF-01 — Navegação anônima
**Regra:** aberto p/ navegar, fechado p/ agir. **Objetos:** Anúncio, Comunidade, Post, Usuário(perfil), ReferênciaPreço (leitura).

**Jornada:** visitante sem login vê feed/listings/comunidades públicas/perfis/posts + busca/filtra; ao clicar em ação → gate "Entre para continuar" → cadastro (UF-02/03) com retorno ao contexto.
```gherkin
Cenário: Anônimo vê e é gateado ao agir
  Dado um visitante sem login num anúncio de doação
  Quando clica em "Pedir"
  Então é levado ao cadastro; após verificar e-mail+WhatsApp, retorna ao anúncio
```

## UF-02 — Cadastro sem cupom
**Objetos:** Usuário, Verificação(e-mail), Verificação(WhatsApp), Credencial/Passkey, Carteira, Comunidade(default).
**Estados:** `iniciado → email_validado → telefone_validado → passkey_criada → safe_gerada → creditada → ativo`

**Jornada:**
1. (Opcional) login social **Google/Apple** pré-preenche nome/e-mail.
2. Nome/apelido, e-mail, telefone, **CEP** (→ ViaCEP automatiza bairro/cidade/estado; HTML5 geolocation opcional). Declara **idade ≥18**.
3. **Avatar auto-gerado** (inicial+cor/DiceBear) se sem foto.
4. **Confirmação dupla:** e-mail (link/token via MailKit+Postfix) **E** telefone (OTP **WhatsApp+SMS via Zenvia**).
5. Cria **passkey** (WebAuthn; sem seed).
6. Anti-sybil (device fingerprint/IP/e-mail/telefone únicos) → gera **Safe** (AA 4337).
7. **Crédito normal: faucet R$20** (sem cupom). Autenticado (JWT) + auto-vínculo à comunidade default da cidade.
```gherkin
Cenário: Cadastro sem cupom recebe crédito normal
  Dado um visitante sem cupom e device/IP dentro dos limites anti-sybil
  Quando preenche nome, e-mail e telefone, valida ambos (e-mail + OTP WhatsApp)
  E cria uma passkey
  Então uma Safe é gerada e o faucet credita R$20 equivalente em RVM
  E é autenticado e vinculado à comunidade default da cidade
```

## UF-03 — Cadastro com cupom
**Objetos:** + Cupom, CouponRedeemer (on-chain). **Estados:** idem UF-02 + `cupom_resgatado`.

**Jornada:** igual à UF-02, mas o usuário **informa um cupom** (opcional). Cupom válido → crédito = **faucet R$20 + valor do cupom** (somado, mint on-chain via `CouponRedeemer`).
```gherkin
Cenário: Cadastro com cupom válido (R$20 + bônus)
  Dado um visitante com cupom "BEMVINDO" válido (maxUses>0, dentro da validade)
  Quando conclui o cadastro (e-mail + WhatsApp verificados, passkey criada)
  Então o faucet credita R$20 + o valor do cupom (somado, on-chain)
  E o cupom é marcado como usado (resgate único por carteira)

Cenário: Cupom inválido não bloqueia o cadastro
  Dado um visitante com cupom expirado/esgotado
  Quando conclui o cadastro
  Então o registro prossegue com crédito normal (R$20) e o cupom é ignorado com aviso
```

## UF-04 — Login
**Objetos:** Credencial/Passkey (ou login social Google/Apple), Usuário.
```gherkin
Cenário: Login por passkey
  Dado um usuário cadastrado
  Quando autentica com a passkey (WebAuthn) ou Google/Apple
  Então recebe JWT e acessa ações (já verificado no cadastro)
```

## UF-05 — Recuperação de conta
**Objetos:** RecuperaçãoConta (admin+timelock), Usuário.
**Estados:** `solicitada → em_timelock → aprovada → executada | negada`
```gherkin
Cenário: Recuperação via admin + timelock
  Dado um usuário que perdeu o acesso à passkey
  Quando solicita recuperação
  Então um pedido entra em timelock; após expirar, o admin (árbitro) aprova e transfere a custódia
```

## UF-06 — Onboarding
**Objetos:** Usuário, Comunidade(default), Anúncio.
```gherkin
Cenário: Auto-vínculo à comunidade da cidade
  Dado um novo usuário com CEP "60000-000"
  Quando conclui o registro
  Então é vinculado à comunidade default da cidade e vê o feed por raio
```

---

# B. Anúncios (5 combinações kind×modo)

> **Objeto comum:** Anúncio (kind+modo) + Categoria + Localização + vendedor(embed) + imagens(MinIO).
> **Diferença-chave:** produto gera **ProductNFT mint-to-escrow** ao publicar; serviço **não** gera token até a compra/voluntariado.

## UF-07 — Produto · trocar
**Objetos:** Anúncio, ProductDetails(condition, stock), ProductNFT(mint-to-escrow), ReferênciaPreço.
**Estados:** `rascunho → ativo (NFT no Vault) → em_andamento → concluído | cancelado`
```gherkin
Cenário: Publicar produto para trocar
  Dado um usuário verificado
  Quando cria anúncio kind=product, modo=trocar, preço RM$ 50, com imagens e localização
  Então um ProductNFT é mintado direto no EscrowVault (mint-to-escrow)
  E o anúncio aparece no feed com comparativo (BRL ↔ mediana RVM ↔ sugestão)
```

## UF-08 — Produto · repassar
**Objetos:** idem UF-07 (preço RVM **baixo**). **Estados:** idem.
```gherkin
Cenário: Repassar acessível
  Dado um usuário verificado
  Quando cria anúncio kind=product, modo=repassar, preço RM$ 15 (abaixo do justo)
  Então o ProductNFT é mintado no Vault e o anúncio aparece com badge "💜 Acessível"
```

## UF-09 — Produto · doar
**Objetos:** Anúncio, ProductDetails, ProductNFT, **Doação/Ajuda**. **Preço 0 RVM.**
**Estados:** `anunciada → pedida → combinada → entregue → confirmada → recompensada | cancelada`
```gherkin
Cenário: Publicar produto para doar
  Dado um usuário verificado
  Quando cria anúncio kind=product, modo=doar (preço 0)
  Então o ProductNFT é mintado no Vault (mesma atomicidade) e aparece com badge "🎁 Doação"
  E entra no fluxo de doação (UF-14)
```

## UF-10 — Serviço · trocar
**Objetos:** Anúncio, ServiceDetails(unitType per-service/horas, duration), ReferênciaPreço. **Sem NFT até a venda.**
**Estados:** `rascunho → ativo → em_andamento → concluído | cancelado`
```gherkin
Cenário: Publicar serviço para trocar
  Dado um usuário verificado
  Quando cria anúncio kind=service, modo=trocar, preço RM$ 30, unitType=per-service
  Então nenhum token é mintado (mint-on-purchase na contratação — UF-13)
  E o anúncio aparece no feed com detalhes de serviço
```

## UF-11 — Serviço · voluntariar
**Objetos:** Anúncio, ServiceDetails, **Doação/Ajuda**. **Preço 0 RVM.** Voucher de voluntariado mintado no aceite.
**Estados:** `anunciada → pedida → combinada → prestada → confirmada → recompensada | cancelada`
```gherkin
Cenário: Publicar serviço para voluntariar
  Dado um usuário verificado
  Quando cria anúncio kind=service, modo=voluntariar (preço 0)
  Então nenhum token é mintado agora (voucher de voluntariado só no aceite — UF-15)
  E aparece com badge "🤝 Voluntário"
```

---

# C. Transações & Ajuda

## UF-12 — Comprar produto
**Objetos:** Troca, EscrowVault, Carteira(buyer), ProductNFT, Avaliação. **Estados:** `ofertada → financiada → entregue → [72h] → liberada|disputada | cancelada → reembolsada`
```gherkin
Cenário: Compra completa (atomic swap)
  Dado comprador com saldo e produto ativo (NFT no Vault)
  Quando paga RM$ 50
  Então RM$ 50 são bloqueados (Block)
  Quando vendedor marca "entregue" e comprador confirma, e 72h sem disputa
  Então atomic swap: vendedor recebe RM$ 49 (−2% Fundo Comunitário), Fundo recebe RM$ 1, NFT→comprador
```

## UF-13 — Contratar serviço
**Objetos:** Troca, ServiceVoucher(mint-on-purchase, 30d), Carteira, Avaliação.
```gherkin
Cenário: Contratação e prestação
  Dado comprador e serviço ativo
  Quando paga RM$ 30
  Então RM$ 30 bloqueados e voucher (30d) mintado ao comprador
  Quando prestado e confirmado (redeem/confirm), 72h sem disputa
  Então vendedor recebe RM$ 29,4 (−2%) e voucher é queimado
Cenário: Voucher expira
  Dado voucher não usado por 30 dias, então RM$ 30 reembolsados (auto-reembolso)
```

## UF-14 — Doar produto
**Objetos:** Doação/Ajuda, PedidoDeAjuda, ProductNFT, Recompensa, PontosDeAjuda, Avaliação.
**Estados:** `anunciada → pedida(fila) → combinada(doador escolhe) → entregue → confirmada → recompensada | cancelada`
```gherkin
Cenário: Doação completa com recompensa multi-eixo
  Dado um doador com produto anunciado modo=doar e 3 pedidos na fila
  Quando o doador escolhe o receptor "Beto" (curadoria: proximidade/reputação/mensagem)
  E Beto recebe o item e confirma
  Então o ProductNFT transfere para Beto (sem movimentar RVM)
  E o doador recebe: +reputação, selo 🎁, bônus de RVM (DonationReward:BonusRvm admin), +pontos de ajuda
Cenário: Doador cancela antes de escolher
  Dado doação anunciada sem receptor escolhido, quando o doador cancela
  Então o NFT é devolvido/queimado do Vault e nenhuma recompensa é creditada
```

## UF-15 — Voluntariar serviço
**Objetos:** Doação/Ajuda, PedidoDeAjuda, ServiceVoucher(voluntariado), Recompensa, PontosDeAjuda.
```gherkin
Cenário: Voluntariado completo
  Dado voluntário com serviço anunciado modo=voluntariar
  Quando escolhe quem ajudar e "contrata" (0 RVM)
  Então um voucher de voluntariado é mintado para rastreabilidade
  Quando a ajuda é prestada e confirmada
  Então o voucher é queimado e o voluntário recebe reputação + selo 🤝 + bônus RVM (admin) + pontos
```

## UF-16 — Transferir RVM P2P
**Objetos:** Transferência, Carteira(from/to).
```gherkin
Cenário: Transferir RVM (inclui "presentear")
  Dado usuário com RM$ 100 disponíveis
  Quando transfere RM$ 20 para outra Safe
  Então RM$ 20 sai e entra na destino (UserOp on-chain, sem taxa no MVP)
```

## UF-17 — Resgatar cupom on-chain
**Objetos:** Cupom, CouponRedeemer, Carteira.
```gherkin
Cenário: Resgate de cupom após cadastro
  Dado um usuário verificado com um cupom válido
  Quando resgata o cupom
  Então o CouponRedeemer valida (maxUses/expiry) e minta RVM extra na sua Safe (resgate único)
```

---

# D. Comunidade
- **UF-18 Criar comunidade** (Comunidade, Membership criador) — criador vira admin da comunidade; geo/interesse/causa; open/private(password).
- **UF-19 Entrar/sair** (Membership) — privadas exigem senha; auto-vínculo na default por cidade.
- **UF-20 Postar/responder** (Post, Reply) — materialized path **depth 6**; moderação em cascata.
- **UF-21 Chat** (Chat/Membership) — SignalR tempo real, **retenção 90 dias**, moderado como posts.
- **UF-22 Denunciar** (Denúncia) — alvo anúncio/post/usuário/chat; **3+ denúncias → auto-ocultar**.
```gherkin
Cenário: Resposta além do limite
  Dado um post em depth 6, quando um membro tenta responder
  Então é sugerido "iniciar nova conversa" (depth excedido)
```

# E. Confiança & Governança
- **UF-23 Avaliar** (Avaliação, Usuário reputação, PontosDeAjuda) — 1–5 por troca/doação finalizada; selos 🎁/🤝.
- **UF-24 Disputar** (Disputa, Troca, Árbitro ARBITRATOR) — só na janela 72h; árbitro libera/reembolsa (on-chain).
- **UF-25 Moderar** (Denúncia, Anúncio/Post/Usuário) — moderador (escopo comunidade) / admin (global); aprovar, ocultar (cascata), banir (temp/perm).

# F. Carteira, Economia & Pricing
- **UF-26 Ver carteira** (Carteira, Transferência, DemurrageEntry, Recompensa) — disponível/bloqueado/histórico (faucet/trocas/doações/P2P/demurrage).
- **UF-27 Demurrage** (DemurrageEntry, ParâmetroSistema, Carteira) — keeper restart-safe/idempotente; preview/run admin; 0,5%/mês acima do piso, queima.
- **UF-28 Comparativo de preço** (ReferênciaPreço, Anúncio) — sempre visível; BRL (ML+seed+comunidade) ↔ mediana RVM ↔ sugestão (Ollama).

# G. Admin & Plataforma
- **UF-29 Gerenciar cupons** (Cupom, CouponRedeemer) — CRUD off-chain + deploy/revogação on-chain (maxUses/expiry).
- **UF-30 Gerenciar parâmetros** (ParâmetroSistema) — faucet, taxa, demurrage, **`DonationReward:BonusRvm` por kind/modo**, validez voucher, janela disputa, retenção chat.
- **UF-31 Dashboard / transparência** (Estatísticas) — admin interno + `revoa.org` (impacto: itens doados, resíduo evitado, prestação de contas do Fundo Comunitário).
- **UF-32 Notificação** (Notificação) — in-app SignalR + Web Push (escrow/ofertas/transferências/posts/chat/doação/preços).

---

## Mapa rápido: fluxo → módulos
| Fluxos | Módulos |
|-------|---------|
| UF-01 navegação | Catalog · Community (read) |
| UF-02/03 cadastro | Identity · Token(faucet/cupom) · Account(Safe) |
| UF-04/05 login/recuperação | Identity |
| UF-07–11 anúncios | Catalog · Token(mint) · PricingIntelligence |
| UF-12/13 trocas | Exchange · Token · Indexer |
| UF-14/15 doação/voluntariado | Exchange · Token(bônus) · Reputation |
| UF-16 P2P · UF-17 cupom | Account · Token |
| UF-18–22 comunidade | Community · Notifications |
| UF-23–25 confiança/gov | Reputation · Moderation |
| UF-26–28 carteira/economia/pricing | Account · Token · PricingIntelligence |
| UF-29–32 admin/plataforma | Token · Moderation · Notifications |

---

*Documento-fonte-de-verdade de fluxos (jornadas + estados + Gherkin). Numeração UF-NN alinhada a `OOUX.md`. Atualizar a cada fluxo novo.*
