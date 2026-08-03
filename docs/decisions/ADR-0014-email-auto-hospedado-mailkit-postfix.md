# ADR-0014: Email transacional auto-hospedado (MailKit + MTA leve, domínio revoa.me)

## Status
Accepted — decisão do dono (rodada cadastro): "SMTP interno bem leve, framework leve, domínio revoa.me".

## Contexto
O cadastro exige **confirmação por e-mail** (link/token) e há notificações transacionais (escrow, posts,
ofertas, redefinição). Sem fins lucrativos → custo de SaaS de e-mail (SendGrid/Mailgun/SES) é atrito.
O dono quer um **servidor SMTP interno, leve, auto-hospedado**, usando o domínio **revoa.me**. Regra de
stack: backend único **.NET** (frameworks leves em .NET).

## Decisão
- **Envio de transacionais:** **MailKit** (biblioteca .NET, MIT — leve, padrão de fato) como cliente SMTP
  no módulo **Identity** (interface `IEmailSender`).
- **MTA local leve (relay/queue):** **Postfix** em container dedicado na **rede interna** (não exposto ao
  Traefik), com **OpenDKIM** para assinatura. MailKit entrega ao Postfix local; Postfix entrega ao mundo.
  - Alternativa 100% .NET: biblioteca `SmtpServer` (cosborne) — viável p/ relay simples, mas Postfix é
    mais maduro p/ entregabilidade/anti-spam. **Recomendação: Postfix** (container leve).
- **Domínio `revoa.me`:** registros DNS (Cloudflare):
  - **MX** → servidor de e-mail (IP do host / serviço de túnel).
  - **SPF:** `v=spf1 mx ~all` (autoriza o MX).
  - **DKIM:** chave pública publicada; OpenDKIM assina a saída.
  - **DMARC:** `v=DMARC1; p=quarantine; ...` (política progressiva).
- **Templates:** transacionais em **Razor/lightweight** (sem dependência pesada); renderizados no backend.
- **Sem SaaS de e-mail** no MVP (custo zero); reavaliar se entregabilidade cair em escala.

## Alternativas consideradas
- **SaaS (SendGrid/Mailgun/SES):** ótima entregabilidade, mas custo + dependência externa (contradiz "interno"). (Rejeitado no MVP.)
- **Servidor SMTP .NET puro (`SmtpServer`):** leve e 100% .NET, mas menos maduro p/ fila/entregabilidade. (Alternativa, não primário.)
- **SMTP do provedor do domínio:** não temos provedor de e-mail; auto-hospedagem é o pedido.

## Consequências
- **+:** custo zero, domínio próprio, controle total, alinhado ao "sem FLP" e ao stack .NET (MailKit).
- **−:** entregabilidade/anti-spam exige configuração cuidada (DKIM/SPF/DMARC, reputação de IP, warmup).
- **−:** operar um MTA (fila, retries, bounce); container Postfix adiciona um serviço ao compose.
- **−:** WhatsApp (Zenvia) cobre a verificação de telefone; e-mail (MailKit+Postfix) cobre a de e-mail — dois canais a manter.
