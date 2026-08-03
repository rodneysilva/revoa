# ADR-0013: Cadastro — campos, verificação dupla e acesso

## Status
Accepted — decisão do dono (rodada cadastro), após benchmark de concorrentes (`MARKET_RESEARCH.md` §9).

## Contexto
O revoa é mainstream + auto-custódia + sem fins lucrativos. O cadastro precisa ser **enxuto/sem fricção**
mas **robusto contra farms** (anti-sybil) e **conformável** (LGPD, idade). Decisões individuais (OTP, idade,
CPF, login social, provedor, avatar, telefone) foram confirmadas pelo dono. Navegação é aberta; ação é gateada.

## Decisão
### Campos
- **Obrigatórios:** nome/apelido · e-mail · telefone · CEP (→ ViaCEP) · passkey (WebAuthn) · aceite Termos+LGPD · idade ≥18 (declaração/DOB) · cupom (opcional).
- **Opcionais:** CPF · avatar (auto-gerado se vazio) · bio · categorias de interesse · foto de capa.

### Verificação dupla (obrigatória no cadastro)
- **E-mail:** link/token via **MailKit + Postfix** (domínio `revoa.me`) — ver ADR-0014.
- **Telefone:** OTP **WhatsApp + SMS** via **Zenvia (TotalVoice) BR**. (Sem SMS pago isolado; WhatsApp primário.)
- Conta só ativa após ambos verificados.

### Demais decisões
- **Idade mínima 18+** (responsabilidade legal plena; auto-custódia/contratos on-chain).
- **CPF opcional** (anti-sybil extra; sem FLP não é obrigatório; dado sensível LGPD).
- **Login social Google + Apple** (além da passkey) — reduz fricção.
- **Avatar auto-gerado** (inicial+cor/DiceBear) se vazio.
- **Cupom opcional:** sem cupom → faucet R$20; com cupom → R$20 + valor do cupom (somado).
- **Anti-sybil:** 1 conta/dispositivo (fingerprint) · 5 contas/IP/dia · e-mail único · telefone único.

### Acesso (anônimo × autenticado)
- **Anônimo:** vê feed/listings/comunidades públicas/perfis/posts; busca/filtra.
- **Autenticado + verificado:** todas as ações (ofertar, pedir/doar, publicar, postar, chat, transferir, avaliar).
- Toda ação que modifica estado exige login + verificação dupla.

## Alternativas consideradas
- **CPF obrigatório:** mais forte anti-fraude, mas fricção + dado sensível. (Rejeitado → opcional.)
- **Telefone como gate progressivo (só p/ anunciar):** rejeitado — dono quer verificação no cadastro.
- **Cadastro invite/cupom-gated:** rejeitado — cupom agora é opcional (captação, não portão).

## Consequências
- **+:** fricção baixa + anti-sybil razoável (device+IP+email+telefone); conformidade LGPD (CPF opcional).
- **+:** navegação aberta atrai tráfego; gate de ação converte visitantes em cadastros.
- **−:** 2 canais de verificação a manter (e-mail MailKit+Postfix + WhatsApp Zenvia).
- **−:** WhatsApp+SMS Zenvia tem custo por mensagem (mitigado: 1 verificação/cadastro).
