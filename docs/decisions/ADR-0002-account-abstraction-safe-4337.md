# ADR-0002: Account Abstraction — Safe + 4337 + webauthn-solidity (carteira invisível)

## Status
Accepted — baseline travado no Plano (§1, decisão #4).

## Contexto
O revoa é self-custody, mas o público-alvo é mainstream (desapegadora, estudante, voluntária) —
**seed phrase é barreira fatal de adoção**. Precisamos de custódia onde o usuário é dono, mas sem
fricção cripto. Gas (ETH) também é fricção: o usuário só deve ver RVM.

## Decisão
**Account Abstraction (ERC-4337):**
- **Safe** (smart account) + **módulo 4337** + **EntryPoint canônico**.
- **Coinbase `webauthn-solidity`** (curva P-256) como signer — login por **passkey (WebAuthn)**, sem seed.
- **Bundler Stackup** para UserOps.
- **Verifying Paymaster** patrocina gas (gas invisível).
- **Recuperação:** privado = admin + timelock; público = guardians (M-de-N).

## Alternativas consideradas
- **EOA + seed phrase:** fricção fatal p/ mainstream; perda de chave = perda de fundos.
- **Custódia centralizada (CEX-like):** quebra o princípio self-custody + enquadra revoa como VASP
  (Lei 14.478, Art. 5 IV — custódia). **Descartado por conformidade e ethos.**
- **Outros signers WebAuthn:** `webauthn-solidity` (Coinbase) é o canônico/auditado.

## Consequências
- **+:** onboarding sem seed (passkey), gas invisível, **self-custody → não-VASP (Art. 5)**.
- **−:** complexidade de contratos (Safe + módulo + recovery); dependência de bunder/paymaster.
- **−:** recuperação privada (admin+timelock) é ponto centralizado agora — aceitável no MVP, removido no público (guardians).
