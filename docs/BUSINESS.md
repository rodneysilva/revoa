# BUSINESS.md — revoa.me / revoa.org

> **Economia circular e de ajuda mútua, sem fins lucrativos, com moeda social comunitária (RVM).**
> Uma comunidade que se ajuda: você oferece o que sabe fazer ou o que não usa mais; o que perdeu
> sentido pra você **encontra um novo lar** onde é útil. Tudo vira **RVM** (crédito de troca) ou pode
> ser **doado/voluntariado de graça**. Hiperlocal, com geolocalização pra conectar quem está perto.

**Projeto:** Plataforma sem fins lucrativos de troca e doação de produtos e serviços (moeda social RVM, auto-custódia)
**Stack:** .NET 10 (backend único) + React/TypeScript (frontend) + Solidity/Foundry + Avalanche Subnet-EVM + MongoDB + MinIO
**Versão:** 3.0.0 (sem fins lucrativos) · **Sucessor/sunset do `trocadeira` Python**
**Domínios:** `revoa.me` = app/plataforma · `revoa.org` = missão, blog, transparência/impacto

---

## 0. O que mudou (leia primeiro)

O revoa **nasceu** como "sem dinheiro" (trocadeira). O mercado provou que o "desejo duplo coincidente"
engasga a circulação. O **pivot travado** é: ***economia circular e de ajuda mútua, sem fins lucrativos,
com moeda social comunitária (RVM)***. **Novo nesta rodada (v3.0):**

| Antes (v2.0) | Agora (v3.0 — sem fins lucrativos) |
|---|---|
| Taxa 2% → "Tesouraria" (lucro implícito) | Taxa 2% → **Fundo Comunitário** (custeia infra, sem lucro, transparente) |
| Natureza jurídica não definida | **Projeto informal (sem CNPJ) por ora**; formalizar associação/OSC antes do público |
| Doação periférica | **Doação = produto de primeira classe** (produto + serviço), coexistindo com troca em RVM |
| Recompensa de doação só reputacional | **Recompensa multi-eixo**: reputação + **bônus de RVM (admin-configurável)** + pontos de ajuda |
| Narrativa "DeFi/cripto" | **Narrativa "moeda social comunitária + ajuda mútua"** (herdeira do Banco Palmas, não do Bitcoin) |
| Um domínio | **Dois**: `revoa.me` (app) + `revoa.org` (missão/transparência) |

> **RVM = moeda social comunitária / crédito de troca, NÃO cripto-investimento.** Não conversível em BRL,
> sem rentabilidade, sem promessa de valorização. **Sem fins lucrativos.** Alinhado à **Lei 14.478/2022**
> (Art. 3, III — utility token) + auto-custódia (não-VASP, Art. 5). Ver `docs/TOKENOMICS.md` e `docs/MARKET_RESEARCH.md`.

---

## 1. Proposta de Valor & Essência

**"Revoar não é voar solto — é ganhar nova utilidade, novo lar."** O que perdeu sentido pra você
encontra quem precisa; o que você sabe fazer financia o que você deseja ter. É uma **comunidade que se
ajuda**, hiperlocal, onde **doar e voluntariar** valem tanto quanto **trocar**.

### Os 4 pilares
1. **Tudo é compartilhado e transformável** — serviço↔produto↔RVM. Nada fica parado; tudo circula e muda de forma.
2. **Reuso com propósito** — o que você não usa mais encontra um novo lar onde é útil (não vai pro lixo).
3. **Habilidade = poder de compra** — seu serviço gera RVM, que compra produtos ou outros serviços.
4. **Ajuda mútua em primeiro lugar** — doar/voluntariar é **celebrado** (recompensa multi-eixo); a economia RVM é meio, não fim.

### Brand voice
- **Tom:** prático, humano, comunitário, brasileiro. Sem jargão. RVM = "crédito de troca" / "moeda da comunidade".
- **Copy seed:**
  - Onboarding: *"Ofereça o que você sabe fazer ou o que não usa mais. Ganhe RVM ou doe de graça."*
  - Produto (trocar): *"Parou de servir pra você? Deixa revoar pra quem precisa."*
  - Produto (doar): *"Não quer RVM? Doe de graça. Ganhe reconhecimento da comunidade."*
  - Serviço: *"Sabe fazer isso bem? Oferece — e use o RVM pra ter o que precisar."*
  - Serviço (voluntariar): *"Tem um tempo? Voluntarie. A comunidade agradece."*
  - Concluída: *"Revoou: encontrou novo lar."*
- **Anti-mensagem (NUNCA):** "invista em cripto", "ganhe dinheiro", "valorização do token", "lucro".

---

## 2. Como Funciona (3 fluxos + 4 modos)

### 2.1 Os 4 modos de um anúncio
Todo **anúncio** tem um `kind` (product | service) e um **modo**:

| Modo | kind | Preço RVM | Natureza | Recompensa ao ofertante |
|------|------|-----------|----------|-------------------------|
| **Trocar** | product · service | valor justo (RVM) | Mercado RVM | RVM recebido + reputação |
| **Repassar** | product | RVM baixo | Acessibilidade | RVM + reputação |
| **Doar** | product | **0 RVM** | Ajuda mútua | **reputação + bônus RVM (admin) + pontos de ajuda** |
| **Voluntariar** | service | **0 RVM** | Ajuda mútua | **reputação + bônus RVM (admin) + pontos de ajuda** |

> **Doar/Voluntariar** são **produto de primeira classe** (como no Buy Nothing). O doador/voluntário
> **não recebe RVM do receptor** (preço 0), mas recebe **recompensa multi-eixo** da comunidade.

### 2.2 Os 3 fluxos econômicos
1. **Moeda → Anúncio:** comprar um produto/serviço com RVM (atomic swap).
2. **Anúncio → Moeda:** oferecer e receber RVM (ao liberar).
3. **Moeda → Conta:** transferência **P2P** de RVM entre carteiras (inclui "presentear" RVM de graça).

> **Doação não é um "fluxo econômico"** (não movimenta RVM do receptor), mas é um **fluxo de ajuda**
> com recompensa própria (multi-eixo). Detalhe em `BUSINESS_RULES.md` §2 e `USER_FLOWS.md`.

### 2.3 Jornada da troca (produto, modo Trocar) — resumo
```
ANUNCIAR  → ProductNFT mintado no EscrowVault (mint-to-escrow; item nunca toca a carteira do vendedor)
OFERTAR   → comprador paga RVM → fundos bloqueados (Block)
ENTREGAR  → vendedor marca entregue
CONFIRMAR → comprador confirma → janela 72h (block.timestamp)
LIBERAR   → atomic swap: RVM (−2% Fundo Comunitário) → vendedor, NFT → comprador
DISPUTAR  → na janela 72h → árbitro (ARBITRATOR) decide
REPUTAÇÃO → ambos se avaliam (1–5)
```
Serviço segue fluxo análogo com voucher (ERC-1155) mint-on-purchase, validade 30d. Detalhes: `ARCHITECTURE.md` · `BUSINESS_RULES.md` · `USER_FLOWS.md`.

---

## 3. O Fluxo de Doação (produto de primeira classe)

> **Doação é central**, não periférica. Coexiste com a troca em RVM, dando ao usuário **escolha**:
> receber crédito de troca **ou** ajudar de graça. Inspirada no **Buy Nothing Project** (14M+ membros).

### 3.1 Doar (produto)
1. **Doador** cria anúncio modo **Doar** (0 RVM) → `ProductNFT` é mintado no EscrowVault (igual ao Trocar — reusa o escrow com valor 0).
2. Interessados **manifestam interesse** (pedem) — há uma **fila de pedidos** com mensagem.
3. **Doador escolhe o receptor** (curadoria humana: por proximidade, reputação, mensagem) — o "matching da doação".
4. Receptor "aceita" (sem pagar RVM) → entrega → confirmação → **NFT transfere ao receptor**.
5. **Recompensa multi-eixo ao doador:** reputação + selo de doador + **bônus de RVM** (configurável admin) + pontos de ajuda.

### 3.2 Voluntariar (serviço)
1. **Voluntário** cria anúncio modo **Voluntariar** (0 RVM) — sem voucher até o aceite.
2. Interessados pedem; **voluntário escolhe** quem ajudar.
3. Ao "contratar" (0 RVM), **voucher de voluntariado** (ERC-1155) é mintado para rastreabilidade;
   prestação → `redeem/confirm` → voucher queimado.
4. **Recompensa multi-eixo ao voluntário:** reputação + selo de voluntário + **bônus de RVM** (admin) + pontos de ajuda.

### 3.3 Recompensa multi-eixo (configurável)
| Eixo | Mecânica | Configurável |
|------|----------|--------------|
| **Reputação/badges** | Avaliação + selos de doador/voluntário (visíveis no perfil) | thresholds admin |
| **Bônus de RVM** | Faucet extra creditado ao doador/voluntário | **`DonationReward:BonusRvm` (por kind/modo) — admin** |
| **Pontos de ajuda** | Contador separado, não conversível; ranking comunitário | peso admin |

> O bônus de RVM é **parâmetro administrativo** — o admin calibra o incentivo à ajuda sem mudar código.

---

## 4. Público-Alvo (personas)

| Persona | Quem é | O que busca |
|---------|--------|-------------|
| **Desapegadora** | Mulher 25–40, B/C, muitos itens parados. Achou a OLX trabalhosa. | Doar de graça ou receber RVM; comunidade. |
| **Faça-você-mesmo** | Homem 30–50, B/C, habilidades manuais e ferramentas. | Oferecer serviços em RVM; voluntariar. |
| **Estudante sustentável** | Jovem 18–28, consciente, pouco dinheiro. | Trocar livros/materiais; valores alinhados. |
| **Pequeno empreendedor solidário** | 25–45, autônomo/MEI. | Divulgar serviços; adquirir insumos da comunidade. |
| **Voluntária comunitária** | 30–55, engajada em ONGs/bairro. | Coordenar doações/voluntariado; impacto visível. |
| **Hiperlocal prático** | Morador de condomínio/bairro. | Coisas perto, sem frete, encontro seguro. |

---

## 5. Diferencial Competitivo (moat)

| Concorrente | Falha | Diferencial revoa |
|-------------|-------|-------------------|
| **OLX / Mercado Livre** | Com fins lucrativos, sem troca/doação estruturada | **Sem FLP** + troca em RVM + doação |
| **Buy Nothing / Freecycle** | Só doação, sem troca com moeda, sem reputação financeira | **Doação E troca** + RVM + reputação on-chain |
| **FB Marketplace / Grupos WA** | Sem histórico, sem garantia, migra p/ WA | Escrow atômico + histórico + chat na plataforma |
| **Banco Palmas** | Offline, local único | **Moeda social digital** escalável, multi-comunidade |
| **Bliive** | Banco de tempo abstrato | Moeda social concreta (RVM) |

**Moat = ajuda mútua (doar/voluntariar) + moeda social comunitária (RVM) + reputação on-chain + hiperlocalidade + sem fins lucrativos.**

---

## 6. Modelo Econômico (sem fins lucrativos)

RVM **não é atrelado ao BRL**. O valor emerge do uso (mediana interna). O **IPCA (+INPC) reajusta
parâmetros** trimestralmente (faucet/cupom + base demurrage + **bônus de doação**).

- **Emissão:** faucet **R$20 equivalente** no cadastro (anti-farming) + cupom/convite (on-chain, maxUses) + mint admin (Fundo Comunitário). **Sem on-ramp.**
- **Demurrage:** 0,5%/mês sobre saldo disponível acima do piso (R$100), base reajustada por IPCA, **queimado**. Isenções: piso, escrow, atividade.
- **Taxa 2% → Fundo Comunitário** (custeia infra/servidores; **sem lucro**, transparente, auditável no `revoa.org`). Do vendedor.
- **Bônus de doação:** creditado ao doador/voluntário, **configurável admin**.
- **Comparativo (sempre visível):** BRL (ML+seed+comunidade) ↔ mediana RVM + sugestão justa (Ollama Qwen 7B).
- **Estimativa BRL em todos os anúncios (valor simbólico):** cada anúncio mostra `≈ R$X` ao lado do preço em RVM — **estimativa simbólica**, não um preço real nem um "câmbio". Serviço: dar **noção de valor** (alinhada à inflação do país) e **evitar engano**, sem transformar o RVM em ativo financeiro. A essência é **trocar** (o RVM é meio, não fim); o valor em BRL é referência discreta. Doações/voluntariado (0 RVM) mostram "Grátis". Taxa pela `Pricing:BrlRate` (admin-configurável, com IPCA).

> **Sem fins lucrativos = sem acionistas, sem distribuição de lucros.** Toda a "receita" (taxa 2%)
> reinverte-se na operação; o `revoa.org` publica a prestação de contas. Detalhes em `TOKENOMICS.md`.

---

## 7. Comunidades (imprescindíveis — no MVP)

A comunidade **é o moat**. Inspirada em Buy Nothing (hiperlocal + gratidão) + Banco Palmas (comunidade real coesa).

- **Default (plataforma)** + **user-created** (criador = admin). Geográficas + geolocalização (ViaCEP + HTML5; raio 1/5/10/25 km Haversine; default por cidade).
- **Hierarquia:** criador → moderadores (escopo da comunidade) → membros.
- **Posts recursivos** (materialized path, depth 6); **chat geral SignalR** (90 dias, moderado como posts).
- **Visibilidade de anúncio:** comunidade | global | ambos. Open vs private (password-gated).
- **"Pedir" (ask)** é tão importante quanto "dar/oferecer" — reciprocidade (lesson Buy Nothing).

---

## 8. Confiança & Governança

| Ação | Quem pode |
|------|-----------|
| Aprovar/rejeitar anúncio/post | Moderador de comunidade (escopo) / Admin (global) |
| Arbitrar disputa de escrow | ARBITRATOR (admin/Safe plataforma) |
| Resolver denúncia / Banir | Moderador (temp) / Admin (perm) |
| Gerenciar categorias/cupons/**parâmetros** | Admin (incl. `DonationReward:BonusRvm`, demurrage, taxa) |

**Roles:** `user`/`mod`/`admin` (+ `creator`/`mod` de comunidade). Reputação 1–5 + selos de ajuda (🌱→⭐→🌟→💎 + doador/voluntário).

---

## 9. Riscos & Mitigações (resumo)

| Risco | Mitigação |
|-------|-----------|
| Escopo MVP (+Comunidades+Doação) | Fases internas; usável na Fase 2 |
| Sustentação sem FLP | Taxa 2%→Fundo Comunitário + doações institucionis (pós-formalização) |
| Lei 14.478 | Utility token + auto-custódia + sem on-ramp + sem FLP; formalizar OSC pré-público |
| Recuperação self-custody | admin+timelock privado; guardians no público |
| Adoção "carteira invisível" | passkey + gas invisível + narrativa "moeda da comunidade" |

Plano completo: Plano-fonte-de-verdade §12.

---

## 10. Roadmap (resumo — detalhe completo em `docs/ROADMAP.md`)

| Fase | Foco | Marco |
|------|------|-------|
| **Fase 0** | Direção + pesquisa + OOUX + identidade | Docs vivos, doação como primeira classe, novos logos |
| **Fase 1** | Infra chain + foundation | Contratos base + Identity/Account/Token/Indexer |
| **Fase 2** | Trocas + Doação + Comunidades | MVP usável: feed, atomic swap, **doação/voluntariar**, comunidade |
| **Fase 3** | Inteligência + confiança | Pricing, demurrage, cupom on-chain, reputação, moderação |
| **Fase 4** | Qualidade + deploy | E2E, observabilidade, cutover revoa.me, `revoa.org` transparência |

---

*Documento-fonte-de-verdade de negócio. Sucessor do `trocadeira/BUSINESS.md`. Para regras operacionais: `BUSINESS_RULES.md`. Para fluxos: `USER_FLOWS.md`.*
