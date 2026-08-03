# USER_FLOWS.md — Fluxos do Usuário (revoa.me)

> **Jornadas + máquinas de estado + user stories (Gherkin).** Documenta **todo o fluxo** do produto,
> do registro ao pós-troca. Formato escolhido: **jornada textual + estados + Dado/Quando/Então**.
> Cada fluxo mapeia para objetos OOUX (`OOUX.md`) e regras (`BUSINESS_RULES.md`).
> **Sem fins lucrativos** · moeda social RVM · auto-custódia.

Relacionados: `BUSINESS.md` · `BUSINESS_RULES.md` · `OOUX.md` · `ARCHITECTURE.md`.

---

## Índice
- [F0 — Navegação anônima (ver tudo, agir exige login)](#f0--navegação-anônima)
- [F1 — Registro (cupom opcional + email/telefone + passkey + Safe + faucet)](#f1--registro)
- [F2 — Onboarding (comunidade default + primeiro anúncio)](#f2--onboarding)
- [F3 — Anúncio de produto](#f3--anúncio-de-produto)
- [F4 — Anúncio de serviço](#f4--anúncio-de-serviço)
- [F5 — Ver carteira (saldo + P2P)](#f5--carteira)
- [F6 — Trocar: comprar produto (atomic swap)](#f6--comprar-produto)
- [F7 — Trocar: contratar serviço (voucher)](#f7--contratar-serviço)
- [F8 — Doar produto](#f8--doar-produto)
- [F9 — Voluntariar serviço](#f9--voluntariar-serviço)
- [F10 — Comunidade (posts recursivos + chat)](#f10--comunidade)
- [F11 — Disputa (árbitro)](#f11--disputa)
- [F12 — Avaliação & reputação](#f12--avaliação)

---

## F0 — Navegação anônima
**Regra:** aberto para navegar, fechado para agir.

### Jornada
1. Visitante (sem login) acessa `revoa.me` → vê o **feed** (listings, por raio/kind/categoria/comunidade).
2. Pode **abrir listings**, ver comunidades públicas, perfis públicos (reputação/selos), posts públicos e o **comparativo de preços**.
3. Pode **buscar/filtrar** sem login.
4. Ao clicar numa **ação** (ofertar, pedir doação/voluntariado, publicar, postar, chat, transferir) → **gate**: "Entre para continuar" → vai ao **cadastro (F1)** preservando o contexto de volta.

### Gherkin
```gherkin
Cenário: Anônimo vê o feed e um anúncio
  Dado um visitante sem login
  Quando acessa revoa.me e abre um anúncio
  Então vê título, descrição, imagens, comparativo e comunidade
  Mas não vê o botão "Oferecer/Pedir" habilitado (apenas "Entre para continuar")

Cenário: Anônimo tenta agir e é levado ao cadastro
  Dado um visitante sem login num anúncio de doação
  Quando clica em "Pedir"
  Então é redirecionado ao cadastro (F1) com retorno ao anúncio após verificar e-mail + WhatsApp
```

---

## F1 — Registro
**Objetos:** Usuário, Carteira, Cupom/Convite · **Anti-sybil:** 1/dispositivo, 5/IP/dia, e-mail único, telefone único.

### Jornada
1. Visitante começa o cadastro (ou via **login social Google/Apple**, que pré-preenche nome/e-mail). **Cupom é OPCIONAL** — pode deixar vazio.
2. Preenche **nome/apelido**, **e-mail**, **telefone**, **CEP** (→ ViaCEP automatiza bairro/cidade/estado; geolocalização via HTML5 opcional). Declara **idade ≥18** (obrigatório).
3. **Avatar auto-gerado** (inicial+cor/DiceBear) se não enviar foto (preenche depois).
4. **Confirmação dupla obrigatória:** valida **e-mail** (link/token) **E telefone** (OTP **WhatsApp+SMS via Zenvia/TotalVoice**). Conta só ativa após ambos.
5. Cria **passkey** (WebAuthn — biometria/dispositivo). **Sem seed phrase.**
6. Backend valida anti-sybil (device fingerprint/IP/e-mail/telefone únicos) → cria **Usuário** + gera **Safe** (Account Abstraction).
7. **Crédito de boas-vindas:** **faucet R$20** sempre; se informou **cupom válido** → + valor do cupom (on-chain, somado).
8. Usuário autenticado (JWT) + auto-vinculado à **comunidade default da cidade** (F2).

### Estados
`iniciado → email_validado → telefone_validado → passkey_criada → safe_gerada → creditada → ativo`

### Gherkin
```gherkin
Cenário: Registro sem cupom (crédito normal)
  Dado um visitante sem cupom
  E que o dispositivo/IP não excederam os limites anti-sybil
  Quando preenche nome, e-mail "ana@x.com" e telefone, valida ambos (e-mail + OTP)
  E cria uma passkey (WebAuthn)
  Então uma Safe é gerada e o faucet credita R$20 equivalente em RVM
  E o usuário é autenticado e vinculado à comunidade default da cidade

Cenário: Registro com cupom (crédito normal + bônus do cupom)
  Dado um visitante com um cupom "BEMVINDO" válido (maxUses>0, dentro da validade)
  Quando conclui o cadastro (e-mail + telefone verificados, passkey criada)
  Então o faucet credita R$20 equivalente + o valor do cupom (somado, on-chain)
  E o cupom é marcado como usado (resgate único por carteira)

Cenário: Cupom inválido não bloqueia o registro
  Dado um visitante com um cupom expirado/esgotado
  Quando conclui o cadastro
  Então o registro PROSSEGUE normalmente (crédito normal R$20)
  E o cupom é ignorado com aviso "cupom inválido ou esgotado"

Cenário: Confirmação dupla obrigatória
  Dado um visitante que validou o e-mail mas não o telefone
  Quando tenta ativar a conta
  Então a ativação é bloqueada até validar o telefone (OTP)
```

> **Campos de cadastro — DECIDIDOS (ver `ADR-0013`):** OTP **WhatsApp+SMS (Zenvia/TotalVoice)** ·
> idade mínima **18+** · **CPF opcional** · login social **Google+Apple** · telefone **obrigatório no cadastro** ·
> avatar **auto-gerado**. Benchmark e automações: `MARKET_RESEARCH.md` §9.

---

## F2 — Onboarding
**Objetos:** Usuário, Comunidade, Anúncio.

### Jornada
1. Após registro, usuário é **auto-vinculado à comunidade da sua cidade** (default).
2. Wizard sugere: completar perfil (nome, avatar, localização via ViaCEP/HTML5) → ver o **feed** (raio 1/5/10/25 km).
3. CTA: "Ofereça o que você sabe ou o que não usa mais" → criar **primeiro anúncio** (F3/F4) ou **doar** (F8).

### Gherkin
```gherkin
Cenário: Auto-vínculo à comunidade da cidade
  Dado um novo usuário com CEP "60000-000" (Fortaleza)
  Quando conclui o registro
  Então é vinculado à comunidade default "Fortaleza"
  E vê o feed priorizando anúncios no raio selecionado
```

---

## F3 — Anúncio de produto
**Objetos:** Anúncio (kind=product), ProductNFT, Categoria · **mint-to-escrow** ao publicar.

### Jornada
1. Usuário clica "Anunciar" → escolhe **kind=produto**, **modo** (trocar/repassar/doar), **categoria**.
2. Preenche: título, descrição, imagens (→ MinIO), condição, estoque/unidades, **localização** (lat/lng, bairro, cidade).
3. Define preço em **RVM** (se trocar/repassar) + vê **comparativo** (BRL, mediana, sugestão).
4. Define **visibilidade** (comunidade/global/ambos) + comunidade-alvo.
5. Ao publicar: `ProductNFT.mintToEscrow()` → NFT nasce no **EscrowVault** (nunca na carteira do vendedor).
6. Anúncio entra no feed (sujeito a moderação se aplicável).

### Estados
`rascunho → ativo (NFT no Vault) → em_andamento → concluído | cancelado`

### Gherkin
```gherkin
Cenário: Publicar produto para trocar
  Dado um usuário autenticado com saldo/comunidade válidos
  Quando cria anúncio kind=product, modo=trocar, preço RM$ 50, com imagens e localização
  Então um ProductNFT é mintado direto no EscrowVault (mint-to-escrow)
  E o anúncio aparece no feed (visibilidade conforme selecionada)
  E o comparativo de preço é exibido (BRL ↔ mediana RVM ↔ sugestão)

Cenário: Produto para doar (0 RVM)
  Dado um usuário autenticado
  Quando cria anúncio kind=product, modo=doar (preço 0)
  Então o ProductNFT é mintado no Vault (mesma atomicidade)
  E o anúncio aparece com badge "🎁 Doação"
```

---

## F4 — Anúncio de serviço
**Objetos:** Anúncio (kind=service) · **sem NFT até a compra** (mint-on-purchase do voucher).

### Jornada
1. Usuário clica "Anunciar" → **kind=serviço**, **modo** (trocar/voluntariar), categoria.
2. Preenche: título, descrição, imagens/portfólio, `unitType` (per-service/horas), duração, validade voucher (30d).
3. Define preço em RVM (se trocar) + comparativo + visibilidade + localização.
4. Publica — **nenhum token é mintado** ainda (o serviço é "virtual" até virar voucher na compra).

### Estados
`rascunho → ativo → em_andamento → concluído | cancelado`

### Gherkin
```gherkin
Cenário: Publicar serviço para trocar
  Dado um usuário autenticado
  Quando cria anúncio kind=service, modo=trocar, preço RM$ 30, unitType=per-service
  Então nenhum token é mintado (mint-on-purchase na contratação)
  E o anúncio aparece no feed com detalhes de serviço
```

---

## F5 — Carteira
**Objetos:** Carteira, Transferência · saldo autoritativo on-chain (Indexer).

### Jornada
1. Header mostra **saldo disponível (RM$)** + badge de não-lidas.
2. "Ver carteira": saldo **disponível**, saldo **bloqueado** (em escrow), **histórico** (faucet/trocas/doações/P2P/demurrage).
3. **Transferir P2P:** escolher destinatário (ou scan/qr/endereço Safe) + valor → UserOp on-chain (sem taxa no MVP). Inclui "presentear" RVM de graça.

### Estados (saldo)
`disponível (+) | bloqueado (em escrow) | queimado (demurrage)`

### Gherkin
```gherkin
Cenário: Transferir RVM P2P
  Dado um usuário com RM$ 100 disponíveis
  Quando transfere RM$ 20 para a Safe de outro usuário
  Então RM$ 20 sai da sua carteira e entra na do destinatário (UserOp on-chain)
  E a transação aparece no histórico de ambos

Cenário: Demurrage reduz saldo parado
  Dado um usuário com RM$ 500 disponíveis (acima do piso R$100) e inatividade
  Quando o keeper executa o demurrage mensal
  Então 0,5% do saldo acima do piso é queimado
  E o evento aparece no histórico como "demurrage"
```

---

## F6 — Comprar produto
**Objetos:** Troca, ProductNFT, Carteira · atomic swap · taxa 2% → Fundo Comunitário.

### Estados
```
ofertada → financiada(buyer.Block) → entregue → [72h] → liberada|disputada | cancelada→reembolsada
```

### Gherkin
```gherkin
Cenário: Compra completa (atomic swap)
  Dado um comprador com saldo suficiente e um produto ativo (NFT no Vault)
  Quando o comprador paga RM$ 50
  Então RM$ 50 são bloqueados (Block) no escrow
  Quando o vendedor marca "entregue" e o comprador confirma
  E passam 72h sem disputa
  Então ocorre o atomic swap: vendedor recebe RM$ 49 (−2% Fundo Comunitário), Fundo Comunitário recebe RM$ 1, NFT vai ao comprador

Cenário: Cancelamento reembolsa
  Dado uma compra financiada ainda não entregue
  Quando o comprador cancela
  Então RM$ 50 são desbloqueados de volta ao comprador e o NFT retorna ao vendedor
```

---

## F7 — Contratar serviço
**Objetos:** Troca, ServiceVoucher · mint-on-purchase · validade 30d.

### Gherkin
```gherkin
Cenário: Contratação e prestação
  Dado um comprador e um serviço ativo
  Quando paga RM$ 30
  Então RM$ 30 são bloqueados e um ServiceVoucher (validade 30d) é mintado ao comprador
  Quando o serviço é prestado e confirmado (redeem/confirm) e passam 72h
  Então o vendedor recebe RM$ 29,4 (−2%), voucher é queimado

Cenário: Voucher expira
  Dado um voucher não usado por 30 dias
  Quando expira
  Então RM$ 30 são reembolsados ao comprador (auto-reembolso)
```

---

## F8 — Doar produto
**Objetos:** Doação/Ajuda, Pedido de Ajuda, ProductNFT, Pontos de Ajuda · valor 0 · recompensa multi-eixo.

### Jornada
1. Doador cria anúncio **modo=doar** (0 RVM) → ProductNFT no Vault.
2. Interessados **pedem** (fila de pedidos com mensagem) — veem o item no feed com badge 🎁.
3. **Doador escolhe o receptor** (curadoria: proximidade, reputação, mensagem).
4. Receptor aceita → entrega → confirmação → **NFT transfere ao receptor** (sem RVM).
5. **Recompensa multi-eixo ao doador:** reputação + selo 🎁 + bônus RVM (admin) + pontos de ajuda.

### Estados
```
anunciada → pedida(fila) → combinada(doador escolheu) → entregue → confirmada → recompensada | cancelada
```

### Gherkin
```gherkin
Cenário: Doação completa com recompensa multi-eixo
  Dado um doador com um produto anunciado modo=doar e 3 pedidos na fila
  Quando o doador escolhe o receptor "Beto" (por proximidade/reputação)
  E Beto aceita, recebe o item e confirma
  Então o ProductNFT transfere para Beto (sem movimentar RVM)
  E o doador recebe: +reputação, selo 🎁 de doador, bônus de RVM (valor do admin), +pontos de ajuda

Cenário: Doação cancelada antes de escolher receptor
  Dado uma doação anunciada sem receptor escolhido
  Quando o doador cancela
  Então o NFT é devolvido/queimado do Vault e nenhuma recompensa é creditada
```

---

## F9 — Voluntariar serviço
**Objetos:** Doação/Ajuda, Pedido de Ajuda, ServiceVoucher (voluntariado), Pontos de Ajuda.

### Jornada
1. Voluntário cria anúncio **modo=voluntariar** (0 RVM) — sem voucher até o aceite.
2. Interessados **pedem ajuda**; voluntário escolhe quem.
3. Ao "contratar" (0 RVM), **voucher de voluntariado** mintado (rastreabilidade); prestado → redeem/confirm → voucher queimado.
4. **Recompensa multi-eixo ao voluntário:** reputação + selo 🤝 + bônus RVM (admin) + pontos de ajuda.

### Gherkin
```gherkin
Cenário: Voluntariado completo
  Dado um voluntário com serviço anunciado modo=voluntariar
  Quando escolhe quem ajudar e "contrata" (0 RVM)
  Então um voucher de voluntariado é mintado para rastreabilidade
  Quando a ajuda é prestada e confirmada
  Então o voucher é queimado e o voluntário recebe reputação + selo 🤝 + bônus RVM (admin) + pontos
```

---

## F10 — Comunidade
**Objetos:** Comunidade, Post (recursivo, depth 6), Chat (Membership), Membership.

### Jornada
1. Usuário entra em comunidades (default por cidade + por interesse/criadas). Cria anúncios visíveis à comunidade.
2. **Posts recursivos:** responde a post (até depth 6); moderação em cascata (ocultar propaga a descendentes).
3. **Chat geral** SignalR (tempo real, 90 dias, moderado como posts).
4. Hierarquia: criador → moderadores (escopo) → membros.

### Gherkin
```gherkin
Cenário: Resposta recursiva dentro do limite
  Dado um post em depth 5 numa comunidade
  Quando um membro responde
  Então o reply é criado em depth 6 (limite máximo)

Cenário: Resposta além do limite é bloqueada
  Dado um post em depth 6
  Quando um membro tenta responder
  Então é sugerido "iniciar nova conversa" (depth 6 excedido)
```

---

## F11 — Disputa
**Objetos:** Disputa, Troca · árbitro ARBITRATOR · janela 72h.

### Gherkin
```gherkin
Cenário: Disputa resolvida pelo árbitro
  Dado uma compra entregue dentro da janela de 72h
  Quando o comprador abre disputa
  Então o árbitro (ARBITRATOR) analisa e decide (libera ao vendedor OU reembolsa o comprador)
  E a decisão é registrada on-chain
```

---

## F12 — Avaliação & reputação
**Objetos:** Avaliação, Pontos de Ajuda.

### Gherkin
```gherkin
Cenário: Avaliação pós-troca/doação
  Dado uma troca/doação confirmada
  Quando cada participante avalia o outro (1–5)
  Então a reputação (média) de ambos é recalculada
  E selos de ajuda (🎁/🤝) e pontos de ajuda são atualizados conforme o caso
```

---

## Mapa rápido: fluxo → módulos
| Fluxo | Módulos envolvidos |
|-------|--------------------|
| F1 Registro | Identity · Token (cupom/faucet) · Account (Safe) |
| F2 Onboarding | Community · Catalog |
| F3/F4 Anúncios | Catalog · Token (mint) · PricingIntelligence |
| F5 Carteira | Account · BlockchainIndexer |
| F6/F7 Trocas | Exchange · Token · Indexer |
| F8/F9 Doação/Voluntariado | Exchange · Token (bônus) · Reputation |
| F10 Comunidade | Community · Notifications |
| F11 Disputa | Moderation |
| F12 Reputação | Reputation |

---

*Documento-fonte-de-verdade de fluxos. Atualizar a cada feature (consultar `OOUX.md` antes de adicionar fluxo).*
