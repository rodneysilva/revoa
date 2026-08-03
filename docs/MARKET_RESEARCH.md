# MARKET_RESEARCH.md — Pesquisa de Mercado revoa.me

> **Economia circular tokenizada (RVM), self-custody, DeFi.** Este documento herda/adapta a pesquisa
> do `trocadeira/MARKET_RESEARCH.md` (C2C/economia circular) e **adiciona** os capítulos
> **DeFi/Web3** e **Lei 14.478/2022** exigidos pelo pivot tokenizado.
> **Data:** 03/08/2026 · **Versão:** 2.0.0 (tokenizada)
> Relacionados: `BUSINESS.md` · `TOKENOMICS.md` · `ARCHITECTURE.md`

---

## Sumário Executivo

O revoa opera na interseção de **três mercados**: (1) **C2C de segunda mão/economia circular**
(R$55–65 bi/ano no Brasil, CAGR ~12%); (2) **comunidades hiperlocais de ajuda mútua** (já existentes
em grupos de WhatsApp, sem rastreabilidade); e (3) **criptoativos/DeFi** (Brasil entre os top do mundo
em adoção, ~16–26 mi de brasileiros com cripto).

O **pivot travado** é resolver o "desejo duplo coincidente" (que paralisa trocas puras sem dinheiro)
com um **crédito de troca on-chain — RVM** — que **não é dinheiro nem ativo financeiro**:
self-custody, sem on-ramp, sem promessa de rentabilidade. O design (utility token + Safe self-custody
+ atomic swap) é o **moat de conformidade** frente à Lei 14.478.

---

## 1. Mercado C2C e Economia Circular (herdado de `trocadeira`)

### 1.1 C2C no Brasil (2024–2025)
- Mercado brasileiro de bens de segunda mão (recommerce): **~R$ 55–65 bi/ano**, **CAGR ~12% a.a.** desde 2020 (OLX Insights + Fordays).
- C2C = **~20–25%** das transações de bens de consumo não-alimentícios.
- "Desapego/Recommerce" cresceu **mais que o dobro** do e-commerce tradicional em 2023–2024.
- **1 a cada 3 brasileiros** já comprou/vendeu item usado online em 2024.
- **~15%** das negociações em classificados usam "aceita troca"; trocas puras (sem dinheiro) ~**2–5%** do volume C2C — o vácuo que o RVM preenche.

### 1.2 Economia circular / sustentabilidade
- Brasil gera **~80 mi de toneladas de resíduos/ano** (ABRELPE); só **~4%** reciclado.
- Tendências: consumo colaborativo, ESG, desbancarização (~34 mi sem conta bancária/limitados), hiperlocalidade (Tem Açúcar?, Nextdoor), custo de vida em alta.

### 1.3 TAM/SAM/SOM (revoa)
| Camada | Estimativa (12 meses) | Premissa |
|--------|----------------------|----------|
| **TAM** | ~10–15 mi de brasileiros que fazem trocas/ajuda mútua online | Internautas 18–55 + interesse em segunda mão/cripto |
| **SAM** | ~2–3 mi | C2C em cidades médias/grandes + dispostos a crédito de troca |
| **SOM** (3 anos) | 50–150 mil ativos | Growth comunitário + cupom/convite |

---

## 2. Competidores (matriz — herdado + tokenizado)

| Plataforma | Modelo | Falha | Diferencial revoa |
|------------|--------|-------|-------------------|
| **OLX** | Compra/venda em dinheiro (70M+ anúncios) | Sem troca estruturada, sem reputação, golpes | Troca tokenizada em RVM, reputação on-chain, escrow seguro |
| **Mercado Livre** | E-commerce B2C+C2C + Mercado Pago | Sem troca; foco comercial | Economia circular comunitária, ajuda mútua |
| **FB Marketplace / Grupos WA** | Troca informal caótica | Sem histórico, sem garantia, migra p/ WA | Escrow atômico + histórico on-chain + chat na plataforma |
| **Enjoei** | Venda de moda (taxa ~20%) | Só moda, só dinheiro | Qualquer categoria, RVM, sem on-ramp |
| **Tem Açúcar? / Nextdoor** | Hiperlocal puro | Só empréstimo/localidade, sem economia | Hiperlocal **+** economia RVM **+** reputação |
| **Bliive** | Banco de tempo (horas abstratas) | Nichado, tempo abstrato | Marketplace direto em RVM (vê o que quer) |
| **Freecycle** | Doação por cidade | UX defasado, sem troca | Doar/voluntariar + troca RVM + reputação |

> **Moat = comunidade hiperlocal + reputação on-chain + crédito de troca fluido + escrow seguro (atomic swap).**

---

## 3. DeFi / Web3 — Mercado e Benchmarks (NOVO)

### 3.1 Adoção de cripto no Brasil
- Brasil está consistentemente entre os **top 10–15** do mundo no **Chainalysis Global Crypto Adoption Index** (2023–2024) — adoção real (não especulativa) alta.
- Estimativas de detentores de cripto no Brasil: **~16–26 milhões** (varia por pesquisa: Gemini, Sherlock/CoinMarketCap, Binance). Crescimento acelerado em 2023–2024, impulsionado por PIX + exchanges locais.
- **PIX como on-ramp de massa:** ~150 mi de usuários; facilitou a entrada no cripto (mas o revoa **não usa on-ramp** — RVM vem de faucet/cupom).
- **CBDC (Drex/Real Digital):** Banco Central pilota moeda digital — sinaliza maturidade regulatória e infra on-chain nacional (relevante para o futuro "público" do revoa).

### 3.2 Tokenização de ativos / community currencies
- **Community/loyalty tokens** crescem como ferramenta de engajamento e economia fechada (Starbucks Odyssey, Reddit Avatares, brand tokens).
- **Lesson Bunz (falido):** moeda interna "BTZ" faliu por modelo financeiro insustentável (rentabilidade implícita + burn de caixa). **O revoa evita isso:** RVM não promete rentabilidade, tem **demurrage** (desincentiva acúmulo), não conversível em BRL.
- **Lesson bancos de tempo (Bliive):** tempo abstrato é difícil de adotar. RVM é concreto: vê o que quer, oferece o que tem.
- **Account Abstraction (ERC-4337):** padrão emergente (2023+) para "carteira invisível" — chave para mass adoption (sem seed phrase). Stack madura: Safe + Stackup + Coinbase webauthn-solidity.

### 3.3 Por que tokenizar ajuda mútua?
1. **Resolve o desejo duplo coincidente** sem dinheiro (RVM conecta qualquer oferta).
2. **Reputação imutável on-chain** — histórico de trocas é ativo público (moat de confiança).
3. **Escrow atômico** — elimina a maior superfície de fraude (item não entregue/pagamento não recebido).
4. **Self-custody** — o usuário é dono do próprio saldo (alinhado ao ethos Web3 + conformidade).
5. **Programabilidade** — demurrage, cupom on-chain, taxa, vouchers com expiração são regras automáticas.

### 3.4 Insights acionáveis (Web3)
1. **Esconder a chain** — usuário vê "RVM/crédito de troca", nunca "cripto/wallet/gas" (carteira invisível).
2. **Passkey > seed phrase** — WebAuthn é o caminho para mainstream (sem fricção de backup).
3. **Gas invisível** (Paymaster) é não-negociável para adoção — usuário nunca vê ETH.
4. **Não prometer rentabilidade** — anti-mensagem absoluta (conformidade + sustentabilidade).
5. **Rede própria agora, pública depois** — mesma chain (chain ID fixo + contratos não-redeployáveis) evita migração dolorosa.
6. **Comparativo de preço transparente** — reduz percepção de "moeda obscura"; o usuário entende o poder de compra.

---

## 4. Lei 14.478/2022 — Análise de Conformidade (NOVO)

> Fonte: texto oficial da **Lei nº 14.478, de 21/12/2022** (planalto.gov.br). Esta é uma **análise de
> produto**, não opinião jurídica formal — validar com advogado antes do lançamento público.

### 4.1 O que a lei regula
A Lei 14.478 dispõe sobre a prestação de serviços de **ativos virtuais** e regula as **prestadoras de
serviços de ativos virtuais (VASPs)**, sujeitas a **autorização prévia** (Art. 2). Define crimes
(fraude com ativos virtuais — Art. 171-A CP, 4–8 anos; PLD/FT — Lei 9.613) e aplica o **CDC** (Art. 13).

### 4.2 "Ativo virtual" (Art. 3) — e a exclusão que salva o RVM
> **Art. 3:** ativo virtual = representação digital de valor negociável/transferível eletronicamente,
> usada para **pagamentos ou investimento**, **EXCETO** [...]
> **III — instrumentos que provejam ao titular acesso a produtos ou serviços especificados ou a
> benefício proveniente desses produtos ou serviços, a exemplo de pontos e recompensas de programas
> de fidelidade.**

**Tese de conformidade do RVM:** o RVM é, por design, um **crédito de troca** que dá ao titular
**acesso a produtos e serviços especificados dentro da plataforma revoa** — análogo a pontos/recompensas
de programa de fidelidade (Art. 3, **III**). **Pilares de design que sustentam a exclusão:**

| Pilar de design | Apoio à exclusão (Art. 3, III) |
|-----------------|-------------------------------|
| **RVM não é conversível em BRL** (sem on-ramp/off-ramp) | Não é "moeda/pagamento" geral — só acesso interno a produtos/serviços |
| **Sem propósito de investimento** (anti-mensagem; demurrage queima, não rende) | Não é "ativo de investimento" |
| **Faucet + cupom + admin mint** (não compra com BRL) | Emissão não financeira; sem captação |
| **"Crédito de troca" (não "cripto/token")** no UX/copy | Reforça natureza de programa de fidelidade |

> Conclusão: o RVM, **como desenhado**, tende a **não se enquadar** como "ativo virtual" (Art. 3) por
> encaixar-se na exclusão do inciso III. Esta é a **espinha dorsal da conformidade**.

### 4.3 VASP (Art. 5) — e como o revoa NÃO é VASP
> **Art. 5:** VASP = pessoa jurídica que, **em nome de terceiros**, executa: troca ativo↔moeda (I),
> troca ativo↔ativo (II), transferência de ativos (III), **custódia/administração** de ativos (IV),
> participação em serviços financeiros/oferta (V).

**Por que o revoa evita a condição de VASP:**
| Atividade VASP (Art. 5) | Como o revoa evita |
|--------------------------|--------------------|
| **Custódia** (IV) | **Self-custody via Safe (AA)** — o revoa **NÃO** custodia ativos dos usuários em nome deles. Cada usuário é dono da própria Safe (passkey). *Este é o moat decisivo.* |
| **Troca ativo↔moeda** (I) | **Sem on-ramp** — RVM não é comprado/vendido por BRL |
| **Transferência** (III) | Transferências P2P são **UserOps assinadas pelo próprio usuário** (não "em nome de terceiros") |
| **Participação em oferta financeira** (V) | Faucet/cupom são emissão de crédito de troca interno, não "oferta pública de ativos" |

> Conclusão: com **self-custody + sem on-ramp + utility token**, o revoa **tende a não ser VASP**
> (Art. 5). Reavaliar se futuramente abrir conversibilidade (stablecoin) — aí sim exigiria autorização.

### 4.4 Obrigações que SEMPRE se aplicam (mesmo fora do regime VASP)
- **CDC (Art. 13):** transparência, publicidade não enganosa, boa-fé objetiva. → O comparativo de preço
  e a clareza "RVM = crédito de troca, não investimento" cumprem isso.
- **Fraude (Art. 171-A CP):** proibido organizar/distribuir carteiras com fraude. → Transparência on-chain
  (Indexer = source of truth) e atomic swap protegem.
- **PLD/FT (Lei 9.613):** mesmo sem ser VASP formal, manter **KYC-lite** (email verificado + anti-sybil)
  e **monitoramento** é prudencial (anti-farming, anti-golpe).
- **LGPD:** dados pessoais (email, localização) seguem a Lei 13.709.

### 4.5 Roadmap de conformidade
1. **MVP privado:** utility token + self-custody + sem on-ramp → baixo risco regulatório.
2. **Pré-público:** parecer jurídico formal sobre enquadramento do RVM (Art. 3, III).
3. **Público (futuro):** se adotar stablecoin BRL 1:1 (lastro+KYC) ou guardians, **reentrar** no escopo
   VASP → buscar autorização do órgão federal (a definir por decreto) + PLD completo.

---

## 5. Comunidades (herdado de `trocadeira/RESEARCH_COMMUNITIES.md`)

> **A comunidade não é módulo a mais — é o moat.** Combina 3 eixos simultaneamente (geográfico +
> interesse + causa), nenhum concorrente faz os três.

| Plataforma | Geográfica | Interesse | Causa | Feed | Reputação local | Moderação comunitária |
|------------|:---:|:---:|:---:|:---:|:---:|:---:|
| FB Marketplace | △ | ✓ | △ | ✓ | ✗ | △ |
| OLX | △ (cidade) | ✗ | ✗ | ✗ | ✗ | ✗ |
| Freecycle | ✓ (cidade) | ✗ | ✓ (ambiental) | ✓ | ✗ | ✓ |
| Vinted | ✗ | ✓ (fóruns) | ✗ | ✓ (follow) | ✗ | ✓ |
| Bunz | ✓ | ✓ | △ | ✓ | △ | ✓ |
| **revoa** | **✓ (bairro)** | **✓ (categoria)** | **✓ (ESG)** | **✓** | **✓ (on-chain)** | **✓** |

- **Onboarding:** auto-vínculo à comunidade da cidade (default).
- **Feed:** prioriza comunidade + raio (1/5/10/25 km Haversine).
- **Posts recursivos** (materialized path, depth 6) + **chat SignalR** (90 dias).
- **A causa (ESG/ajuda mútua):** doar/voluntariar = bônus reputacional.

---

## 6. Métricas-Chave (KPIs)

### 6.1 Aquisição & engajamento
- Usuários cadastrados (alvo 12m: 5.000), DAU (500), taxa de ativação (≥40% criam listing em 7d).
- Listings ativos (2.000), ofertas enviadas (3.000), taxa de resposta ≤48h (≥75%).
- **Trocas finalizadas** (750), taxa de conclusão (≥65%), nota média (≥4.3).

### 6.2 Econômicas (RVM)
- Volume RVM circulante, taxa média de demurrage aplicada, cupons resgatados, transferências P2P.
- **ComparaRVM:** % de listings com comparativo exibido (meta 100%).

### 6.3 Comunidade & impacto
- Posts/recursivos, mensagens de chat (retenção 90d), comunidades ativas, itens em circulação,
  resíduo/CO₂ evitado (estimativa).

---

## 7. Riscos (resumo — Plano §12 + Web3)

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Enquadramento regulatório do RVM | Alto | Utility token + self-custody + sem on-ramp (Art. 3,III / Art. 5); parecer jurídico pré-público |
| Safe+4337+WebAuthn (contratos) | Alto | Componentes canônicos auditados; forge test/coverage; security review |
| Adoção "carteira invisível" falha | Médio | Passkey + gas invisível + comparativo transparente |
| Demurrage rejeitado pelos usuários | Médio | Piso generoso (R$100), isenção por atividade, copy clara |
| PricingIntelligence (API ML+LLM GPU) | Médio | Fase 3; ToS ML; cache; Qwen 7B GPU |
| Bunz-like (falência por modelo financeiro) | Médio | RVM sem rentabilidade + demurrage + não-convertível |

---

## 8. Conclusões Estratégicas

1. **O vácuo é real:** trocas puras sem dinheiro são ~2–5% do C2C — paralisadas pelo desejo duplo coincidente. O RVM desbloqueia isso **sem virar dinheiro**.
2. **O design é o moat de conformidade:** utility token (Art. 3, III) + self-custody (não-VASP, Art. 5) + sem on-ramp = perfil regulatório baixo no MVP privado.
3. **A comunidade é o moat de retenção:** três eixos + reputação on-chain + hiperlocalidade — nenhum concorrente combina tudo.
4. **A invisibilidade da chain é o moat de adoção:** passkey, gas invisível, "crédito de troca" — mainstream sem fricção cripto.
5. **O caminho público existe e é incremental:** mesma chain + autorização VASP + stablecoin (futuro), sem rewrite.

---

*Documento-fonte-de-verdade de mercado. Sucessor do `trocadeira/MARKET_RESEARCH.md`. Análise da Lei 14.478 baseada no texto oficial (planalto.gov.br) — validar com parecer jurídico antes do público.*
