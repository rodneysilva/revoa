# BUSINESS.md — revoa.me

> **Economia circular e de ajuda mútua, tokenizada.** Não é "jogar fora" — é **dar um novo lar
> ao que você tem** e **um novo significado ao que você sabe fazer**, quando alguém precisa.
> Um produto parado encontra nova utilidade; um serviço específico ganha novo propósito.
> Tudo vira **RVM** (ou pode ser **doado/voluntariado** de graça). É uma **comunidade que se ajuda**,
> hiperlocal, com geolocalização para conectar quem está perto.

**Projeto:** Plataforma DeFi de troca de produtos e serviços (tokenizada, self-custody)
**Stack:** .NET 10 + Solidity/Foundry + Avalanche Subnet-EVM + MongoDB + React + MinIO
**Versão:** 2.0.0 (pivot tokenizado) · **Sucessor/sunset do `trocadeira` Python**

---

## 0. O Pivot (leia primeiro)

O revoa **nasceu** como "plataforma sem dinheiro" (trocadeira). O mercado provou que o
"desejo duplo coincidente" (só troca quem quer o que você tem **e** tem o que você quer) engasga
a circulação. O **pivot travado** é: ***economia circular tokenizada com RVM*** — uma moeda de
crédito de troca on-chain que conecta ofertas sem fricção, mas **sem ser dinheiro nem ativo financeiro**.

| Antes (trocadeira) | Agora (revoa) |
|---|---|
| Troca direta "teu por meu" | **RVM** conecta qualquer oferta a qualquer oferta |
| Sem moeda → paralisia sem match | Crédito de troca fluido entre todos |
| Reputação informal | Reputação on-chain + histórico imutável |
| Sem custódia | **Self-custody** (Safe + passkey, sem seed phrase) |
| Sem custódia de item | **Escrow atômico** (NFT/voucher trava o item até a troca) |
| Doar / repassar | **Doar / voluntariar (0 RVM) + repassar (RVM baixo)** |

> **RVM = "crédito de troca", NÃO cripto-investimento.** RVM é meio de troca interno, não conversível
> em BRL no MVP, sem rentabilidade. Alinhado à **Lei 14.478/2022** (não é ativo financeiro,
> não é on-ramp, não promete valorização). Ver `docs/TOKENOMICS.md` e `docs/MARKET_RESEARCH.md`.

---

## 1. Proposta de Valor & Essência

**"Revoar não é voar solto — é ganhar nova utilidade, novo lar."** O que perdeu sentido pra você
encontra quem precisa; o que você sabe fazer financia o que você deseja ter. Você não precisa saber
fazer tudo: oferece o que sabe, recebe RVM, e obtém o resto da comunidade.

### Os 4 pilares
1. **Tudo é compartilhado e transformável** — serviço↔produto↔RVM. Nada fica parado; tudo circula e muda de forma.
2. **Reuso com propósito** — o que você não usa mais encontra um novo lar onde é útil (não vai pro lixo, não fica guardado).
3. **Habilidade = poder de compra** — seu serviço específico gera RVM, que compra produtos ou outros serviços.
4. **RVM é meio, não fim** — a moeda conecta ofertas; o foco é o que cada usuário tem a oferecer.

### Brand voice (alimenta copy/layout/UX — ver `docs/VISUAL_IDENTITY.md`)
- **Tom:** prático, humano, comunitário, brasileiro. Sem jargão crypto. RVM = "crédito de troca".
- **Copy seed:**
  - Onboarding: *"Ofereça o que você sabe fazer ou o que não usa mais. Ganhe RVM. Use pra ter qualquer coisa aqui."*
  - Produto: *"Parou de servir pra você? Deixa revoar pra quem precisa."*
  - Serviço: *"Sabe fazer isso bem? Oferece — e use o RVM pra ter o que precisar."*
  - Troca concluída: *"Revoou: encontrou novo lar."*
  - Transformação: *"Um serviço virou produto. Tudo se transforma."*
- **Anti-mensagem (NUNCA):** "invista em cripto", "ganhe dinheiro", "valorização do token".

---

## 2. Como Funciona (3 fluxos + 4 modos)

### 2.1 Os 4 modos de um anúncio
Todo **anúncio** tem um `kind` (product | service) e um **modo**:

| Modo | kind permitido | Preço RVM | Natureza |
|------|----------------|-----------|----------|
| **Trocar** | product · service | valor justo em RVM | Mercado RVM — recebe crédito de troca |
| **Repassar** | product | RVM baixo | Acessibilidade — abaixo do "preço justo" |
| **Doar** | product | **0 RVM** | Ajuda mútua — ganha bônus reputacional |
| **Voluntariar** | service | **0 RVM** | Ajuda mútua — ganha bônus reputacional |

> **Doar/Voluntariar** resolvem o problema do item parado sem exigir contrapartida; geram
> **bônus reputacional** (incentivo à ajuda mútua). O RVM é opcional — a generosidade sempre circula.

### 2.2 Os 3 fluxos econômicos
1. **Moeda → Anúncio** — comprar um produto/serviço com RVM (atomic swap).
2. **Anúncio → Moeda** — oferecer o que você tem e receber RVM.
3. **Moeda → Conta** — transferência P2P de RVM entre carteiras (self-custody).

### 2.3 A jornada de uma troca (produto)
```
1. ANUNCIAR   vendedor lista produto → ProductNFT é mintado direto no EscrowVault (mint-to-escrow)
              o item nunca toca a carteira do vendedor; nasce "em custódia"
2. OFERTAR    comprador paga RVM → fundos são bloqueados (Block) no escrow; NFT continua no Vault
3. ENTREGAR   vendedor marca entregue (ou físico, ou combinam logística hiperlocal)
4. CONFIRMAR  comprador confirma recebimento → abre janela de 72h (block.timestamp)
5. LIBERAR    após 72h sem disputa → atomic swap: RVM (−2% taxa) → vendedor, NFT → comprador;
              2% → Tesouraria
   DISPUTAR   na janela de 72h → árbitro (role ARBITRATOR) decide
6. REPUTAÇÃO  ambos se avaliam (1–5); bônus reputacional se foi doar/voluntariar
```
Serviço segue fluxo análogo, mas o **voucher** (ERC-1155) é mintado **ao comprar** (mint-on-purchase),
`redeem`/`confirm` libera, validade **30d** com auto-reembolso + claim via árbitro.

Detalhes técnicos: `docs/ARCHITECTURE.md` · `docs/BUSINESS_RULES.md`.

---

## 3. Público-Alvo (personas)

| Persona | Quem é | O que busca no revoa |
|---------|--------|----------------------|
| **Desapegadora** | Mulher 25–40, B/C, muitos itens parados (roupas, decoração, livros). Achou a OLX trabalhosa. | Anunciar rápido, receber RVM, usar pra ter o que precisa — sem dinheiro. |
| **Faça-você-mesmo** | Homem 30–50, B/C, habilidades manuais (marcenaria, elétrica, mecânica) e ferramentas. | Oferecer serviços em RVM e "comprar" produtos ou outros serviços. |
| **Estudante sustentável** | Jovem 18–28, B/C/D, consciente, pouco dinheiro, muita disposição. | Trocar livros/materiais/eletro; comunidade com valores alinhados. |
| **Pequeno empreendedor** | 25–45, autônomo/MEI (design, aulas, comida caseira, fotografia). | Divulgar serviços, receber RVM, adquirir insumos da comunidade. |
| **Voluntária comunitária** | 30–55, engajada em grupos de bairro/ONGs. | Coordenar doações/voluntariado locais; reputação rastreável; impacto visível. |
| **Hiperlocal prático** | Morador de condomínio/bairro que quer coisas perto (sem frete, encontro seguro). | Feed por raio (1/5/10/25km); comunidade do bairro; confiança por proximidade. |

### Onde estão
- **Cidades médias e grandes** (capitais + regiões metropolitanas): densidade de ofertas.
- **Comunidades de bairro / condomínios**: trocas hiperlocais fortalecem vizinhança e cortam logística.
- **Universidades**: troca natural de livros, materiais, serviços entre estudantes.
- **Grupos de WhatsApp**: onde a ajuda mútua já acontece, mas sem rastreabilidade nem reputação.

---

## 4. Diferencial Competitivo (moat)

| Concorrente | Modelo | Falha | Diferencial revoa |
|-------------|--------|-------|-------------------|
| **OLX** | Compra/venda em dinheiro | Sem troca estruturada, sem reputação, golpes | Troca tokenizada em RVM, reputação on-chain, escrow seguro |
| **Enjoei** | Venda de moda (taxa ~20%) | Só moda, só dinheiro, taxa alta | Qualquer categoria, RVM, sem on-ramp, sem taxa de listagem |
| **FB Marketplace / Grupos WA** | Troca informal caótica | Sem histórico, sem garantia, migra p/ WA | Escrow atômico + histórico on-chain + chat na plataforma |
| **Tem Açúcar? / Nextdoor** | Hiperlocal puro | Só empréstimo/localidade, sem economia | Hiperlocal **+** economia RVM **+** reputação |
| **Bliive** | Banco de tempo (horas abstratas) | Nichado, tempo abstrato, pouco usado | Marketplace direto em RVM: vê o que quer, oferece o que tem |
| **Bunz (falido)** | Moeda interna "BTZ" | Modelo financeiro insustentável faliu | RVM = crédito de troca **não financeiro**, sem promessa de rentabilidade |

**O moat do revoa = comunidade hiperlocal + reputação on-chain + crédito de troca fluido + escrow seguro.**
Nenhum concorrente combina os três eixos de comunidade (geográfico + interesse + causa) com
self-custody e atomic swap.

> Análise completa em `docs/MARKET_RESEARCH.md` (TAM/SAM/SOM, matriz de competidores, benchmarks).

---

## 5. Modelo Econômico (RVM)

RVM **não é atrelado ao BRL**. O valor RVM↔BRL **emerge** do uso (mediana interna). O que é
**reajustado por IPCA (+INPC) trimestralmente** são **parâmetros** (faucet/cupom + base do demurrage),
não o valor do token.

- **Emissão:** faucet **R$20 equivalente** no cadastro (anti-farming: 1/dispositivo, 5/IP/dia) + cupom/convite (on-chain, maxUses → mint) + mint admin. **Sem on-ramp real.**
- **Demurrage:** 0,5%/mês sobre saldo disponível acima do piso (R$100 equiv), **base reajustada por IPCA trimestralmente**, **queimado** (desincentiva acúmulo, incentiva circulação). Isenções: piso, saldo em escrow, atividade recente.
- **Taxa:** 2% por troca finalizada, do vendedor, creditada à **Tesouraria**.
- **Comparativo (sempre visível):** BRL de mercado (API ML + seed admin + comunidade) ↔ mediana RVM dos listings + sugestão de preço justo. Normalização por **Ollama Qwen 7B**.

Detalhes em `docs/TOKENOMICS.md`.

---

## 6. Comunidades (imprescindíveis — no MVP)

A comunidade **não é módulo a mais — é o moat.** O revoa já nasce dentro de comunidades reais
(grupos de WhatsApp, bairros, universidades); o trabalho é **digitalizar** com reputação e ajuda mútua.

- **Default (plataforma)** + **user-created** (criador = admin).
- **Geográficas** (condomínio/bairro/cidade/região) + **geolocalização** (lat/lng via ViaCEP + HTML5; feed por raio 1/5/10/25km Haversine; comunidades default por cidade).
- **Hierarquia:** criador → moderadores (nomeados, agem em anúncios+posts da comunidade) → membros.
- **Posts recursivos** (materialized path, depth 6) — resposta a resposta até 6 níveis.
- **Chat geral em tempo real** (SignalR, 90 dias de retenção, moderado como posts).
- **Visibilidade de anúncio:** comunidade | global | ambos.
- **Open vs private** (password-gated).

Design de comunidades: `docs/MARKET_RESEARCH.md` (cap. Comunidades) + `docs/BUSINESS_RULES.md`.

---

## 7. Confiança & Governança

| Ação | Quem pode | Efeito |
|------|-----------|--------|
| **Aprovar/rejeitar anúncio** | Moderador de comunidade (no contexto da comunidade), Admin (global) | Moderação de conteúdo |
| **Arbitrar disputa de escrow** | ARBITRATOR (admin/Safe plataforma) | Libera RVM ou reembolsa, na janela de disputa |
| **Resolver denúncia** | Moderador de comunidade / Admin | Ignorar, remover, banir |
| **Banir (temp/permanente)** | Moderador (temp), Admin (perm) | Desativa listings, bloqueia login |
| **Gerenciar categorias/cupons** | Admin | CRUD + cupom on-chain (maxUses/expiry) |

**Roles:** `user` / `mod` / `admin` (+ `creator`/`mod` de comunidade). Reputação 1–5 por troca, com
níveis/badges (🌱 Iniciante → ⭐ → 🌟 → 💎 Lenda).

---

## 8. Riscos & Mitigações (resumo)

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Escopo MVP amplo (+Comunidades) | Alto | Fases internas; usável na Fase 2 |
| Safe+4337+WebAuthn+recovery (contratos) | Alto | Componentes canônicos auditados; forge test/coverage; security review |
| Mint-to-escrow + atomic swap | Alto | forge test de todos os caminhos + fuzzing |
| Consistência on/off-chain | Alto | Indexer = source of truth; idempotência txHash+logIndex; confirmações |
| Recuperação self-custody + mainstream | Alto | admin+timelock privado; guardians no público |
| Regulação (Lei 14.478) | Médio | MVP privado + sem on-ramp + não-convertível; sem rentabilidade |
| YAGNI violado | Médio | Gate de review + CI estática; OOUX só com objetos de CTA/estado reais |

Plano completo de riscos: Plano §12.

---

## 9. Roadmap (resumo — detalhe no Plano §11)

| Fase | Foco | Marco |
|------|------|-------|
| **Fase 0** | Direção + pesquisa + OOUX + identidade visual | Docs/memória vivos, mapa OOUX, 6 explorações visuais |
| **Fase 1** | Infra de chain + foundation | Nó + contratos base + módulos Identity/Account/Token/Indexer |
| **Fase 2** | Núcleo de trocas + Comunidades | MVP usável: feed, atomic swap, comunidade (posts+chat), frontend |
| **Fase 3** | Inteligência + econômico + confiança | Pricing, demurrage, cupom on-chain, reputação, moderação |
| **Fase 4** | Qualidade + deploy privado | E2E, observabilidade, cutover revoa.me (sunset trocadeira), PWA |

---

*Documento-fonte-de-verdade de negócio. Sucessor do `trocadeira/BUSINESS.md`. Para arquitetura: `ARCHITECTURE.md`. Para regras operacionais: `BUSINESS_RULES.md`.*
