# ADR-0005: Mint-to-escrow (produtos) + mint-on-purchase (serviços)

## Status
Accepted — baseline travado no Plano (§1, decisão #2).

## Contexto
Produtos são tokenizados (ERC-721) e serviços têm voucher (ERC-1155). A pergunta: **quando** o token
é mintado e **onde** mora? O escrow precisa ser **atômico** (troca irrevogável RVM↔item). Mintar no
momento errado cria risco (item na carteira do vendedor durante a venda = dupla venda/fraude) ou
complexidade (mover NFT entre carteiras).

## Decisão
Dois padrões opostos, por kind:
- **Produto (ERC-721):** `ProductNFT.mintToEscrow()` **ao listar**. O NFT **nasce dentro do EscrowVault**
  — nunca toca a carteira do vendedor. Na compra, só o **RVM se move** (atomic swap simplificado); o
  NFT já está no Vault e transfere ao comprador na liberação.
- **Serviço (ERC-1155):** `ServiceVoucher` mintado **ao comprar** (`mint-on-purchase`) → carteira do
  comprador. `redeem`/`confirm` libera; validade 30d → auto-reembolso; queimado na liberação.

## Alternativas consideradas
- **Mint ao vendedor, transfer ao comprar (produto):** risco de dupla venda (vendedor pode transferir
  o NFT pra fora durante a venda); mais steps no swap.
- **Voucher mint-on-purchase para ambos:** produto não é "consumível" como serviço; NFT único (721) faz
  mais sentido p/ item físico singular.
- **Sem tokenização (off-chain):** perde reputação on-chain + atomic swap + programabilidade.

## Consequências
- **+:** atomic swap de produto move só RVM (NFT já no Vault) → mais simples e seguro.
- **+:** impossível dupla venda de produto (NFT nunca saiu do Vault enquanto listado).
- **−:** dois lifecycles para manter (cuidado para não misturar — armadilha documentada no AGENTS.md).
- **−:** cancelamento de listing de produto precisa devolver/queimar o NFT que estava no Vault.
