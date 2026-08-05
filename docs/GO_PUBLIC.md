# revoa.me — Caminho para o Público + Formalização

> **Status atual: PRIVADO (dev).** Este documento lista o que precisa estar pronto **antes** de abrir
> a plataforma ao público (`revoa.me`). É um checklist vivo — cruze antes do cutover público.
>
> Princípio: **sem fins lucrativos, moeda social comunitária, ajuda mútua**. O público só quando o
> produto estiver robusto, auditável e com a base jurídica clara.

---

## 1. Pré-requisitos técnicos (antes do público)

- [ ] **Rede:** subir a **Subnet-EVM própria** (dev = anvil/local; público = subnet real com
      validadores próprios). RPC público somente leitura + archive.
- [ ] **Contratos auditados:** auditoria de segurança (RVM, ProductNFT, ServiceVoucher, EscrowVault,
      CouponRedeemer). `forge coverage` ≥ limite definido. Multisig no `DEFAULT_ADMIN_ROLE`.
- [ ] **Account Abstraction real:** trocar o EOA MVP (chave em texto) por **Safe + ERC-4337 +
      EntryPoint + bundler (Stackup) + verifying paymaster + webauthn/passkey (P-256, Coinbase)**.
      "Carteira invisível" sem chave privada exposta. (ADR-0002.)
- [ ] **Gasless:** Paymaster custeia o gas (usuário não precisa de ETH). Definir política/sustos.
- [ ] **Quórum de Árbitros:** role `ARBITRATOR_ROLE` com **multisig/quórum** (não uma só carteira)
      para resolver disputas. Auditoria pública de decisões.
- [ ] **Guardians:** recuperação social (guardians) para o self-custody público (substitui o
      "recuperação admin+timelock" do privado).
- [ ] **Indexer robusto:** projeção de eventos on-chain → read models **idempotentes** (txHash+logIndex),
      confirmações anti-reorg. Source of truth on-chain.
- [ ] **Anti-sybil em escala:** 1/dispositivo, 5/IP/dia, e-mail+telefone únicos, captcha/Proof-of-Work
      no cadastro. Rate-limit no faucet/cupom/resgate.
- [ ] **E-mail real:** SMTP relay (SendGrid/Mailgun/Amazon SES) — Postfix local NÃO entrega em caixa real.
- [ ] **WhatsApp/SMS real:** Zenvia em produção (OTP/Magic Link).
- [ ] **Login social:** Google + Apple (OAuth) — além do passkey.
- [ ] **Observabilidade:** OTLP exporter → collector (Tempo/Jaeger); alertas; dashboards.
- [ ] **Backups + DR:** MongoDB (snapshot periódico), MinIO, estado da chain.
- [ ] **LGPD/GDPR:** consentimento, retenção, deleção (direito ao esquecimento), DPA de terceiros.
- [ ] **Termos de uso + Política de privacidade** claros (RVM = utility token, não ativo financeiro;
      auto-custódia; sem garantia).

## 2. Econômico / Governança

- [ ] **IPCA-trimestral automático:** Quartz reajusta parâmetros (faucet/cupom/base demurrage/bônus de
      doação) pelo IPCA(+INPC). Atualmente manual.
- [ ] **Demurrage scheduler:** keeper periódico (mensal) com janela e copy clara (piso R$100).
- [ ] **Pricing pipeline completo:** ML market prices (Mercado Livre) + IPCA + Ollama; agendamento semanal.
- [ ] **Transparência pública:** `revoa.org` com prestação de contas (taxa 2% → Fundo Comunitário),
      impacto, parâmetros, referências de preço.
- [ ] **Fundo Comunitário:** governança do uso da taxa 2% (infra + iniciativas da comunidade) — sem lucro.

## 3. Formalização jurídica

- [ ] **Natureza jurídica:** formalizar **associação/OSC** (sem CNPJ hoje — projeto informal). Estatuto,
      assembleia, conselho.
- [ ] **Parecer jurídico pré-público:** enquadramento da Lei 14.478/2022 (RVM = utility token Art.3 III;
      não-VASP Art.5). Risco cambial (RVM não atrelado ao BRL). Disclaimer "não é investimento".
- [ ] **Compliance:** KYC leve (CPF opcional hoje → definir no público), PLD conforme a lei.
- [ ] **Seguros/garantias:** definir responsabilidade (self-custódia = usuário dono das chaves).

## 4. Comunidade / Operação

- [ ] Moderadores + árbitros treinados; fluxo de denúncia/ban; curadoria de doação.
- [ ] Seed de comunidades âncora (ONGs, coletivos) por cidade.
- [ ] Documentação do usuário (FAQ, como funciona) + onboarding.
- [ ] Plano de comunicação (revoa.org = blog/transparência/impacto).

---

## Ordem sugerida do cutover público (quando tudo acima estiver ✔)

1. **Cutover privado → "beta fechado"** (convites/cupom): `revoa.me` deixa o Python (trocadeira) → app novo; `revoa.org` = transparência. Acesso por convite (cupom on-chain).
2. **Beta aberto** (login social + passkey + verificação dupla): público navega, ação exige verificação.
3. **Público pleno** (subnet mainnet de settlement, validadores próprios, guardians, stablecoin BRL opcional para on/off-ramp — fora do escopo atual).

> **Risco regulatório principal:** tratar RVM como moeda/ativo financeiro. Mitigação: narrativa
> "crédito de troca comunitário + ajuda mútua", sem on-ramp, auto-custódia, sem promessa de retorno.
> Sempre alinhado à Lei 14.478 e à natureza sem fins lucrativos.
