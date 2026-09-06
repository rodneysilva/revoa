# ADR-0018: Role ARBITRATOR para resolução de disputas de escrow

## Status
Accepted — hotfix de segurança + correção definitiva (Fase 1 da refactoração).

## Contexto
`POST /api/trades/{id}/resolve` move fundos do escrow on-chain e estava protegido apenas por
`Policy = "Verified"` — **qualquer usuário verificado podia resolver qualquer disputa de
qualquer pessoa** e direcionar os fundos. O TODO no código admitia a falta do gate. A role
`ARBITRATOR_ROLE` já existia no contrato EscrowVault, mas o backend assinava o resolve com a
chave do faucet e sem distinção de quem autorizou.

## Decisão
- **UserRole ganha `Arbitrator`** (append no fim do enum — persiste int32 no Mongo,
  storage-safe).
- **JWT carrega claims `role`** (`RoleClaimType = "role"`); policy `"Arbitrator"` =
  `RequireRole("Arbitrator", "Admin")` — **admins são árbitros por padrão**, role dedicada
  cobre árbitros leigos convidados sem poder admin.
- `/resolve` exige a policy; `ResolvedBy` (e-mail/identidade do token) vai no comando para
  auditoria.
- Gestão da role via `PUT /api/admin/users/{id}/role` (Policy Admin).
- A chave que assina `resolve` on-chain é dedicada (env var), separada da chave do faucet.

## Alternativas consideradas
- **`Policy = "Admin"` simples (o hotfix):** suficiente para travar o furo, mas acopla
  arbitragem a admin — o modelo de negócio prevê árbitros da comunidade.
- **NFT/assinatura off-chain por disputa:** over-engineering para o estágio atual.

## Consequências
- **+:** furo fechado com granularidade correta (árbitro ≠ admin); trilha de auditoria no comando.
- **−:** gestão de árbitros é manual (endpoint admin) até houver fluxo de convite.
- **−:** quem assina on-chain continua sendo custodial (ver ADR-0015) — auditoria off-chain é
  a fonte de quem autorizou o quê.
