# VISUAL_IDENTITY.md — revoa.me

> **Identidade visual tokenizada.** Sucessora do `trocadeira/VISUAL_IDENTITY.md`. O pivot para
> economia circular tokenizada (RVM) mantém o DNA (esmeralda/sky, dark-first, "revoar") e adiciona
> a camada **precisa/on-chain** (malha, pulso, "RM$"). O **slogan é o nome "revoa.me"** — wordmark
> como hero, **sem tagline** (as frases soavam artificiais).
>
> Explorações: [`VISUAL_EXPLORATIONS.md`](VISUAL_EXPLORATIONS.md) + 6 PNGs em
> [`visual-explorations/`](visual-explorations/).

---

## 0. Resumo dos 3 Elementos de Marca (travados)

| Elemento | Forma | Uso |
|----------|-------|-----|
| **Wordmark** | **`revoa.me`** — uma palavra contínua (sem espaço antes do `.me`), Inter/WorkSans ExtraBold, gradiente esmeralda→sky | Logo principal, header, hero |
| **Monograma** | **`RV`** — ícone compacto em círculo/arco; serve de favicon, avatar, marcador on-chain (nó) | Favicon, PWA, app icon, nó da malha |
| **Símbolo monetário** | **`RM$`** — prefixo do RVM (ex.: `RM$ 20`); peso bold, acento sky | Saldo, preços, comparativo, carteira |

> **Sem tagline.** O wordmark `revoa.me` carrega a marca. Copy de contexto aparece **no produto**
> (onboarding, hero, estados), nunca como "slogan" colado ao logo.

---

## 1. As 6 Explorações (resumo — ver PNGs)

### Bloco A — Híbridas (comunidade + calor humano)
1. **[Circuito Vivo](visual-explorations/01-circuito-vivo.png)** — órbitas concêntricas; esmeralda→sky + âmbar. *Ciclo com alma.* ★ base do wordmark
2. **[Bairro Solar](visual-explorations/02-bairro-solar.png)** — grade de mapa + pinos + raios (1/5/10/25km); terracota/creme. *Hiperlocalidade.* ★ motivo do feed/raio
3. **[Semente & Voo](visual-explorations/03-semente-voo.png)** — trajetórias de asa; esmeralda+lima+dourado. *Transformação.* ★ motivo da "troca concluída"

### Bloco B — Web3 (on-chain / DeFi nativo)
4. **[Malha On-Chain](visual-explorations/04-malha-onchain.png)** — graph de nós; ink + nós esmeralda/sky; Tektur. *A rede é a verdade.* ★ motivo do indexer/blockchain
5. **[Ledger Prismático](visual-explorations/05-ledger-prismatico.png)** — shards triangulares; roxo→rosa→sky. *Tokenização é refração.* ★ motivo de NFT/voucher
6. **[Pulso Subnet](visual-explorations/06-pulso-subnet.png)** — mínimo suíço + linha de pulso; ink + sky cirúrgico. *Precisão silenciosa.* ★ base da UI minimal

---

## 2. Direção Final (síntese)

A identidade final **combina** o calor híbrido (**Circuito Vivo**) com a precisão Web3
(**Pulso Subnet** + **Malha On-Chain**). Não escolhemos uma única exploração — escolhemos um
**sistema** onde cada motivo tem seu lugar:

| Contexto | Motivo (exploração de origem) |
|----------|-------------------------------|
| **Logo / wordmark** | gradiente esmeralda→sky + arco "revoando" (Circuito Vivo) |
| **Monograma "RV"** | círculo com arco orbital (Circuito Vivo) / nó central (Malha On-Chain) |
| **Feed & mapa (raio)** | grade + pinos + raios 1/5/10/25km (Bairro Solar) |
| **Carteira / saldo "RM$"** | rótulo clínico (Pulso Subnet) |
| **Troca/escrow tracker** | linha de pulso/onda do estado (Pulso Subnet) |
| **NFT/voucher** | facets/prisma (Ledger Prismático) |
| **Indexer/blockchain** | nós/arestas (Malha On-Chain) |
| **Sucesso ("Revoou")** | trajetória de asa (Semente & Voo) |

> **Princípio unificador:** dark-first + gradiente esmeralda→sky + precisão suíça. O calor humano
> vem do gradiente e das curvas; a confiança técnica vem do grid e da tipografia controlada.

---

## 3. Cores

### Primária (mantida do trocadeira)
| Cor | Hex | Uso | Tailwind |
|-----|-----|-----|----------|
| **Esmeralda** | `#10b981` | Ação primária, wordmark, trocar | `emerald-500` |
| **Sky** | `#0ea5e9` | Fim do gradiente, links, indexer | `sky-500` |
| **Roxo** | `#a855f7` | NFT/voucher, refração | `purple-500` |
| **Rosa** | `#ec4899` | Repassar, facets, badges | `pink-500` |

### Gradientes oficiais
```
linear-gradient(135deg, #10b981 0%, #0ea5e9 100%)   /* wordmark + ação primária */
linear-gradient(135deg, #a855f7 0%, #ec4899 100%)   /* NFT/voucher */
```

### Acentos novos (tokenização)
| Cor | Hex | Uso |
|-----|-----|-----|
| **Âmbar** | `#f59e0b` | "pouso"/chegada, comparativo BRL |
| **Terracota** | `#bc6c25` | hiperlocal/bairro, doar |
| **Lima** | `#84cc16` | transformação, voluntariar |

### Neutros (dark-first, mantidos)
| Cor | Hex | Uso |
|-----|-----|-----|
| **Ink** | `#0a0a0a` | background principal |
| **Charcoal** | `#171717` | cards |
| **Smoke** | `#262626` | borders, divisores |
| **Silver** | `#737373` | texto secundário |
| **White** | `#fafafa` | texto principal |

---

## 4. Tipografia

| Contexto | Fonte | Peso | Obs. |
|----------|-------|------|------|
| **Wordmark logo** | Inter | 800 (ExtraBold) | `revoa.me` uma palavra; gradiente |
| **Headings** | Inter | 700–800 | 24–48px |
| **Corpo** | Inter | 400–500 | 14–16px |
| **Botões/CTA** | Inter | 600–700 | 14px |
| **Monoespaçado (dados/tx)** | Tektur ou mono | 400 | saldo `RM$`, hashes, blocos |
| **Microcopy/labels** | Inter | 400 | 12px, uppercase, tracking + |

Fonte primária: **Inter** (Google Fonts). Na plataforma: WorkSans como fallback próximo.
Para dados on-chain/saldos, fonte monoespaçada (Tektur ou `ui-monospace`) reforça a precisão técnica.

---

## 5. Monograma "RV" (regras de construção)

- **Forma:** "RV" dentro de um **círculo** com um **arco orbital** aberto (sugere revoar/circulação).
- **Gradiente:** esmeralda→sky no arco; "RV" em branco (`#fafafa`) sobre fundo ink, ou ink sobre fundo claro.
- **Tamanhos mínimos:** 24px (ícone) / 120px (completo).
- **Versões:** fundo escuro (ink), fundo claro (white), mono (sky) para favicon/PWA.

> O monograma **é o nó central** da "Malha On-Chain" — pode ser animado como nó pulsante na UI.

---

## 6. Símbolo Monetário "RM$"

- **Sempre prefixo:** `RM$ 20` (nunca `20 RM$`).
- **Peso bold**, cor sky (`#0ea5e9`) em saldos; silver (`#737373`) em secundário.
- **Comparativo** usa `≈ R$ X` (BRL) ao lado, em silver menor — informativo, não cotação.
- Em fonte monoespaçada quando em tabelas/hashes.

---

## 7. Componentes UI (badges por modo)

| Modo | Badge | Cor | Ícone |
|------|-------|-----|-------|
| Trocar | 🔄 Troca | Esmeralda | arco circular |
| Repassar | 💜 Acessível | Rosa | — |
| Doar | 🎁 Doação | Terracota | — |
| Voluntariar | 🤝 Voluntário | Lima | — |

### Badges de reputação (mantidos)
| Nível | Emoji | Cor |
|-------|-------|-----|
| Iniciante | 🌱 | neutral |
| Trocador | ⭐ | sky |
| Trocador Pro | 🌟 | amber |
| Lenda | 💎 | purple |

### Estado de escrow (troca tracker)
Linha de pulso (Pulso Subnet): `offered → funded → delivered → [72h] → released|disputed`,
com o ponto atual pulsando em sky.

---

## 8. Copy & Tom (alimenta UX — ver `BUSINESS.md`)

| Contexto | Copy |
|----------|------|
| Onboarding | *"Ofereça o que você sabe fazer ou o que não usa mais. Ganhe RVM. Use pra ter qualquer coisa aqui."* |
| Produto | *"Parou de servir pra você? Deixa revoar pra quem precisa."* |
| Serviço | *"Sabe fazer isso bem? Oferece — e use o RVM pra ter o que precisar."* |
| Troca concluída | *"Revoou: encontrou novo lar."* |
| Transformação | *"Um serviço virou produto. Tudo se transforma."* |

**Substituições obrigatórias:** "cripto/token" → **RVM/crédito de troca** · "comprar/vender" →
**trocar/repassar/doar** · "carteira/carteira" → **carteira** (invisível, sem seed) · "transação" → **troca**.

**Anti-mensagem (NUNCA):** "invista em cripto", "ganhe dinheiro", "valorização do token", "mineração".

---

## 9. Favicons & Assets

| Arquivo | Uso |
|---------|-----|
| `logo.svg` | Header, login — wordmark `revoa.me` |
| `icon.svg` | Favicon, PWA — monograma `RV` |
| `favicon.ico` | Browser tab — `RV` 32×32 |
| `icon-512.png` | PWA install — `RV` 512 |

> Assets finais a serem produzidos na Fase 2 (frontend). As 6 explorações (`visual-explorations/`)
> são a referência para o designer/geração vetorial.

---

## 10. Regras de Uso do Logo

- `revoa.me` é **sempre uma palavra** — sem espaço entre "revoa" e ".me".
- Sempre sobre fundo ink (`#0a0a0a`) ou white (`#fafafa`).
- Não distorcer, não mudar a proporção do gradiente (135° esmeralda→sky).
- Não adicionar tagline/slogan ao logo.
- Respiro mínimo = altura do "R".

---

## 11. Versão e Manutenção

- **Versão:** 4.0 (pivot tokenizado; 6 explorações)
- **Criado em:** 03/08/2026
- **Referenciado em:** `BUSINESS.md`, `AGENTS.md`, `ARCHITECTURE.md`
- **Explorações:** `VISUAL_EXPLORATIONS.md` + `visual-explorations/*.png`

---

*Documento-fonte-de-verdade de identidade visual. Sucessor do `trocadeira/VISUAL_IDENTITY.md`. Assets finais (SVG/PWA) na Fase 2.*
