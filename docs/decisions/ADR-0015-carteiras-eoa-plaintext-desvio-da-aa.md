# ADR-0015: Carteiras EOA plaintext como desvio temporário da Account Abstraction

## Status
Accepted — desvio explícito do ADR-0002 (AA/Safe/4337), a ser revertido antes do público.

## Contexto
ADR-0002 travou carteira invisível via **Safe + ERC-4337 + EntryPoint canônico + bundler Stackup
+ webauthn-solidity (passkey P-256)**. Na prática das Fases 1–3 do desenvolvimento isso ainda
não existe: integrar bundler/paymaster + módulo 4337 no MVP custava semanas antes de qualquer
fluxo de troca funcionar ponta a ponta.

O que o código faz hoje: o módulo **Account** cria uma **EOA (chave privada em plaintext no
MongoDB)** por usuário no cadastro/ativação; a carteira é invisível para o usuário (mesma UX
prometida pela AA), e o backend assina as transações (purchase/redeem/release/faucet) com essas
chaves via Nethereum, contra os contratos Foundry em `contracts/` (anvil/Subnet-EVM).

## Decisão
Formalizar o desvio (em vez de nota espalhada): **EOA plaintext managed-custody** como modelo
provisório.
- Aceitável **enquanto**: chain privada/dev, sem valor real, usuários de teste conhecidos.
- **Bloqueador para público**: mover para AA (ADR-0002) ou, no mínimo, KMS/encryption-at-rest
  das chaves + rotação; jamais subir chaves plaintext para uma chain pública.
- A chave do faucet (anvil dev) e a role `ARBITRATOR_ROLE` do EscrowVault já são separadas
  (env vars), não a mesma chave.

## Alternativas consideradas
- **Implementar Safe+4337 agora:** custo alto, atrasa todo o resto; o contrato de UX (carteira
  invisível) é preservado pelo backend assinando.
- **Sem carteira no MVP (ledger off-chain puro):** descartado — o estado financeiro
  autoritativo on-chain é decisão central (ADR-0003/0005).

## Consequências
- **+:** fluxos on-chain ponta a ponta (mint/escrow/release) funcionando e testados desde cedo.
- **−:** débito técnico de segurança explícito — rastrear em ROADMAP como pré-requisito de
  abertura pública.
- **−:** migração futura EOA→Safe exige lifecycle (drenar/mover fundos) planejado.
