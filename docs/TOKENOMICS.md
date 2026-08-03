# TOKENOMICS.md — RVM (ReVoA Money)

> **RVM é "crédito de troca", NÃO ativo financeiro.** Meio de troca interno, on-chain (ERC-20),
> self-custody, gas invisível. Não conversível em BRL no MVP, sem rentabilidade, sem promessa de
> valorização. **Alinhado à Lei 14.478/2022.** O foco é o que o usuário **oferece**; o RVM é meio, não fim.

---

## 1. Natureza do RVM

| Atributo | Definição |
|----------|-----------|
| **Ticker** | `RVM` · exibição **RM$** (ex.: `RM$ 20`) |
| **Padrão** | ERC-20 (componível), com roles `MINTER`/`BURNER` |
| **Chain** | Avalanche Subnet-EVM própria (mesma chain do privado→público) |
| **Gas** | Invisível — patrocinado por **Paymaster** (usuário só vê RVM) |
| **Custódia** | **Self-custody** via Account Abstraction (Safe + passkey, sem seed phrase) |
| **Atrelamento ao BRL** | **NÃO.** O valor RVM↔BRL **emerge** do uso (mediana interna). |
| **Conversibilidade** | Não no MVP (sem on-ramp/off-ramp real). Futuro público: stablecoin BRL 1:1 (lastro+KYC). |

> **Por que não atrelar ao BRL?** Atrelar criaria um "ativo financeiro" sujeito à regulação da Lei 14.478
> como valor mobiliário. RVM é **moeda de utilidade/credito de troca** — seu valor prático vem da
> liquidez das ofertas na plataforma, não de um lastro externo. O **comparativo BRL é informativo**,
> sempre exibido para o usuário entender o "preço justo", mas não é uma cotação garantida.

---

## 2. Emissão (como RVM entra em circulação)

| Fonte | Mecanismo | Anti-abuso |
|-------|-----------|------------|
| **Faucet de cadastro** | **R$20 equivalente** mintados na Safe do novo usuário | 1/dispositivo · 5 contas/IP/dia · cadastro **invite/cupom-gated** |
| **Cupom/Convite (on-chain)** | `CouponRedeemer.redeem()` → mint de RVM | `maxUses` + `expiry` on-chain · resgate único por carteira |
| **Mint admin (Tesouraria)** | Operação autorizada para seed/eventos | Auditoria on-chain · só role `MINTER` |

> **Sem on-ramp real:** RVM não é comprado com BRL. Toda emissão vem de faucet/cupom/admin.
> Isso mantém o RVM fora do escopo de "oferta pública de ativos" da regulação.

### Cupom = Convite (unificado, ON-CHAIN)
- Contrato **`CouponRedeemer`** valida cupom → minta RVM direto na Safe do resgatante.
- Campos on-chain: `code` (hash), `amount`, `maxUses`, `expiry`, `usedBy[]`.
- Anti-farming off-chain complementar: rate-limit por IP/dispositivo + validação de email.
- Admin faz CRUD de cupons (off-chain CRUD + deploy/revogação on-chain).

---

## 3. Demurrage (desincentivo ao acúmulo → incentivo à circulação)

> Blueprint: `equivale` (`DemurrageService` + `DemurrageSchedulerHostedService` + `DemurrageEntry` ledger).

O demurrage **queima** uma fração do saldo **disponível** (fora de escrow) periodicamente, incentivando
a circulação em vez do acúmulo especulativo. É a contrapartida que mantém o RVM saudável como moeda
de troca (não reserva de valor).

### Regras
| Parâmetro | Valor base | Reajuste |
|-----------|-----------|----------|
| **Taxa** | **0,5%/mês** | **base reajustada por IPCA (+INPC) trimestralmente** |
| **Piso de isenção** | **R$100 equivalente** (saldo abaixo não é taxado) | Reajustado por IPCA trimestralmente |
| **Base de cálculo** | Só **saldo disponível** (NÃO saldo bloqueado em escrow) | — |
| **Inatividade** | Isenta se atividade recente (definir janela) | — |
| **Destino** | **Queimado** (`burn`) — reduz oferta | — |

### Execução (keeper)
- `DemurrageSchedulerHostedService` (BackgroundService Quartz), **restart-safe e idempotente**
  (tracking de última execução por período).
- `PreviewAsync` (prévia do mês) + `ApplyAsync` (executa, lança no `DemurrageEntry` ledger).
- Admin: `GET /api/admin/demurrage/preview` + `POST /api/admin/demurrage/run`.

### Por que IPCA reajusta a *base* (não o valor do RVM)?
O IPCA mede a inflação em BRL. Como o piso e a taxa base são expressos em "R$ equivalente",
reajustá-los por IPCA mantém o **poder de compra real** da isenção e a **severidade real** do
demurrage ao longo do tempo. **O IPCA NÃO controla o valor de mercado do RVM** — ele só evita que a
inflação corroa os parâmetros do sistema.

---

## 4. IPCA (+INPC) — Oracle Trimestral

| Aspecto | Definição |
|---------|-----------|
| **Frequência** | Trimestral (job via webfetcher → IBGE) |
| **Índices** | **IPCA** (principal) + **INPC** (complementar) |
| **O que reajusta** | **Parâmetros**: faucet (R$20 equiv) · cupom (amounts) · piso do demurrage · base do demurrage |
| **O que NÃO reajusta** | Valor de mercado do RVM (emerge do uso) |
| **Transparência** | Página pública de parâmetros (`docs/PRICING.md`, Fase 3) com histórico |

> Implementação: `PricingIntelligence` webfetcher consome API IBGE → job aplica reajuste →
> registra ADR/mudança de parâmetro. Reprovável por admin em caso de anomalia.

---

## 5. Taxa de Transação

- **2%** por troca finalizada (config: `TransactionFee:Percent`).
- **Cobrada do vendedor:** `sellerPayout = total − fee`.
- **Creditada à Tesouraria** (Safe da plataforma, role `MINTER`/`ARBITRATOR`).
- Visível no painel admin: `TotalFeesCollected`, `TotalVolume`, taxa média.
- **Sem taxa de listagem** — publicar é grátis (incentiva a oferta).

---

## 6. Comparativo de Preço (sempre visível)

O usuário **sempre** vê um referencial para tomar decisões justas:

```
BRL de mercado  ↔  Mediana RVM interna  ↔  Sugestão justa
(API ML + seed admin + comunidade)   (mediana dos listings da categoria)   (Ollama Qwen 7B)
```

- **BRL de mercado:** API Mercado Livre + valores seed admin + contribuições da comunidade.
  `webfetcher` só para **IPCA/IBGE** (não há scraping de ML — usamos a API oficial).
- **Mediana RVM:** agregação dos listings ativos por categoria (off-chain, PricingIntelligence).
- **Sugestão justa:** **Ollama Qwen 7B (GPU)** normaliza as fontes → sugere faixa RVM justa.
- **Refresh semanal** (Quartz) para BRL/mediana; **trimestral** para IPCA.

> **Anti-mensagem:** o comparativo é **informativo**, não uma cotação garantida. RVM não tem
> "preço" — tem uma referência de poder de compra. Detalhes: `docs/PRICING.md` (Fase 3).

---

## 7. Ciclo de Vida do RVM (sumário)

```
EMISSÃO                          USO                              REMOÇÃO
─────────                        ────                             ───────
faucet R$20 ─┐                  atomic swap ─┐                  demurrage (queima)
cupom on-chain ─┼─→ mint RVM ─→ Safe ─→ P2P ─┤─→ taxa 2% ─→ Tesouraria
mint admin ──┘                  (escrow)     │                  (não volta)
                                            └─→ vendedor recebe sellerPayout
```

- **Saldo autoritativo ON-CHAIN** (Indexer projeta → read models).
- **Block/Release** no escrow: `buyer.Block(total)` ao pagar; `seller.Credit(sellerPayout)` + `treasury.Credit(fee)` ao liberar.

---

## 8. Anti-Sybil & Anti-Farming

| Vetor | Defesa |
|-------|--------|
| Múltiplas contas por pessoa | Cadastro **invite/cupom-gated** (maxUses) + email verificado + **1 conta/dispositivo** |
| Farms por IP | **5 contas/IP/dia** (rate-limit) |
| Faucet farming | Cupom `maxUses`/`expiry` on-chain + resgate único por carteira |
| Acúmulo especulativo | **Demurrage** (queima saldo parado acima do piso) |
| Demurrage farming (atividade fake) | Isenção só com atividade **real** (troca/post qualificado) |

---

## 9. Futuro (público — FORA do MVP)

- **Stablecoin BRL 1:1** com lastro auditável + KYC (conversibilidade real).
- **Guardians (M-de-N)** para recuperação self-custody sem admin centralizado.
- **Subnet L1 de settlement** em mainnet (dev = nó local; mainnet no launch público).
- **Quorum do ARBITRATOR** público (Safe multisig descentralizado).
- Nestes casos, reavaliar enquadramento na Lei 14.478 (ativos / prestador de serviços de ativos virtuais).

---

## 10. Parâmetros Atuais (vivos — manter atualizado)

| Parâmetro | Valor | Origem |
|-----------|-------|--------|
| Faucet de cadastro | R$20 equivalente | Plano §14 |
| Demurrage | 0,5%/mês | Plano §11 (resolvido) |
| Piso demurrage | R$100 equivalente | Plano §11 |
| Taxa de transação | 2% | Plano §11 |
| Validade voucher serviço | 30 dias | Plano §1 (#7) |
| Janela de disputa | 72h (`block.timestamp`) | Plano §1 (#6) |
| Chat retenção | 90 dias | Plano §14 |
| Posts profundidade | 6 níveis (materialized path) | Plano §14 |
| Raio de feed | 1/5/10/25 km (Haversine) | Plano §8 |

*Última revisão de parâmetros: Fase 0. Próximo reajuste IPCA: a definir na Fase 3.*
