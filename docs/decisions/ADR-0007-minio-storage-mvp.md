# ADR-0007: MinIO para storage no MVP (IPFS só ao abrir público)

## Status
Accepted — baseline travado no Plano (§1, decisão #19).

## Contexto
NFTs (`ProductNFT`) precisam de `tokenURI` apontando para metadata + imagens. Opções de storage
descentralizado: IPFS/Arweave (público, imutável, sem dono) vs storage centralizado (S3-like).
No MVP **privado**, a imutabilidade/descentralização do IPFS é custo/complexidade sem benefício
imediato (pinagem, gateways, latência). Mas o `tokenURI` deve ser **estável e migrável**.

## Decisão
**MinIO (S3-compatible)** em container dedicado no compose do revoa, roteado via Traefik
(`assets.revoa.me`). O `tokenURI` aponta para o MinIO. **IPFS só ao abrir público** (com migração
planejada: `tokenURI` como indireção para permitir troca de storage sem quebrar o NFT).

## Alternativas consideradas
- **IPFS desde o início:** pinagem (Pinata/ próprio), gateways, latência, custo — sem benefício no privado.
- **Storage proprietário sem padrão S3:** acoplamento; MinIO (S3) permite troca fácil para S3 real depois.
- **On-chain metadata (data URI):** caro (gas) e inflexível para imagens.

## Consequências
- **+:** controle total, baixa latência, sem dependência de pinners, S3-compatible (portável).
- **+:** roteado pelo Traefik existente (consistência com a infra central `rodne/infra`).
- **−:** centralizado agora (ponto único da plataforma) — aceitável no privado; migrar a IPFS no público.
- **−:** `tokenURI` deve ser projetado como **indireção** (resolver → storage) para permitir migração sem quebrar NFTs.
