# ADR-0001: Avalanche Subnet-EVM própria (privada→pública, mesma chain)

## Status
Accepted — baseline travado no Plano (§1, decisão #1).

## Contexto
O revoa precisa de uma chain para RVM (ERC-20), NFTs/vouchers e escrow. Opções: L1 pública existente
(Ethereum/Polygon/Avalanche C-Chain), testnet (Fuji), rollup/L2, ou **Subnet própria**. O MVP é
privado (acesso controlado em beta; cupom opcional), mas o plano é abrir público depois. Migrar de chain depois
(contratos não-redeployáveis, saldos, history) é caro e arriscado.

## Decisão
Usar uma **Avalanche Subnet-EVM própria**, **privada agora → pública depois, na mesma chain**.
- Chain ID fixo desde o início.
- Contratos implantados uma única vez (não-redeployáveis).
- Dev = nó local; mainnet pública no launch.

## Alternativas consideradas
- **L1 pública (Polygon/Avalanche C-Chain):** exige gas em token externo, custos, exposição prematura.
- **Fuji testnet:** ok p/ dev, mas não é "a mesma chain" do público → migração dolorosa.
- **Rollup/L2:** complexidade desnecessária no MVP; custos/sequencer.

## Consequências
- **+:** soberania total, gas invisível (próprio Paymaster), sem migração público, custos controlados.
- **−:** operar/validar o nó; segurança inicial depende da plataforma (privado). Lançar público exige
  validadores descentralizados (deferido).
- **Neutro:** chain ID fixo + EntryPoint canônico travados desde já.
