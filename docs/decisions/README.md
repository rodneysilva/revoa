# Decisões de Arquitetura (ADRs)

> Architecture Decision Records. Cada ADR registra uma **decisão** com seu contexto, alternativas e
> consequências. Imutável após "Accepted" (nova decisão = novo ADR que o supersede). Formato:
> `ADR-NNNN-titulo.md`.
>
> As decisões "travadas" do Plano-fonte-de-verdade (§1) são registradas aqui como baseline.

## Índice

| ADR | Título | Status |
|-----|--------|--------|
| [ADR-0001](ADR-0001-subnet-evm-propria.md) | Avalanche Subnet-EVM própria (privada→pública, mesma chain) | Accepted |
| [ADR-0002](ADR-0002-account-abstraction-safe-4337.md) | Account Abstraction: Safe + 4337 + webauthn-solidity (carteira invisível) | Accepted |
| [ADR-0003](ADR-0003-monolito-modular-mongodb-isolado.md) | Monólito modular com MongoDB isolado por coleção | Accepted |
| [ADR-0004](ADR-0004-rvm-utility-token-lei-14478.md) | RVM como utility token (não ativo financeiro) — Lei 14.478 | Accepted |
| [ADR-0005](ADR-0005-mint-to-escrow-mint-on-purchase.md) | Mint-to-escrow (produtos) + mint-on-purchase (serviços) | Accepted |
| [ADR-0006](ADR-0006-react-vite-spa.md) | Frontend React (Vite SPA) em vez de SolidJS | Accepted |
| [ADR-0007](ADR-0007-minio-storage-mvp.md) | MinIO para storage no MVP (IPFS só ao abrir público) | Accepted |
| [ADR-0008](ADR-0008-ooux-objects-first.md) | OOUX objects-first (ORCA) como metodologia de design | Accepted |

## Como escrever um ADR

```
# ADR-NNNN: Título
## Status
## Contexto (por que decidir agora? forças em jogo)
## Decisão (o que decidimos)
## Alternativas consideradas
## Consequências (positivas, negativas, neutras)
```
