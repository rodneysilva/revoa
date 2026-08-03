# SMART_CONTRACTS.md — revoa.me (Marco M2 — Foundry)

> Smart contracts on-chain do revoa (Solidity 0.8.24 + Foundry). Economia circular tokenizada
> (RVM) com atomic swap de produtos (ERC-721) e serviços (ERC-1155), escrow custódia, taxa 2% →
> Fundo Comunitário (sem fins lucrativos) e doação/voluntariado (valor 0).
>
> **Stack:** Solidity `0.8.24` · OpenZeppelin Contracts `5.7.0` · Foundry `forge 1.7.1` (Docker).
> **Local dos arquivos:** `contracts/` (`src/`, `test/`, `script/`, `lib/`).
> Referências: `ARCHITECTURE.md` §5 · `BUSINESS_RULES.md` §2 · ADR-0005/0009/0010 · `ROADMAP.md` (Fase 1→M2).

---

## 1. Visão geral dos contratos

| Contrato | Padrão | Responsabilidade |
|----------|--------|------------------|
| **`RVM`** | ERC-20 + `AccessControl` | Crédito de troca (utility token, Lei 14.478). Roles `MINTER_ROLE`/`BURNER_ROLE`. |
| **`Treasury`** | `AccessControl` | **Fundo Comunitário** (sem fins lucrativos). Recebe a taxa 2%; `withdraw` só admin (custeio de infra). |
| **`ProductNFT`** | ERC-721 (`ERC721URIStorage`) + `AccessControl` | Produto tokenizado. **`mintToEscrow`** ao listar (NFT nasce no Vault). |
| **`ServiceVoucher`** | ERC-1155 + `AccessControl` | Voucher de serviço. **`mintOnPurchase`** ao comprar; `redeem`/`burn`; validade; não transferível. |
| **`EscrowVault`** | Atomic swap + `AccessControl` + `ReentrancyGuard` + `IERC721Receiver` | Custódia atômica RVM↔NFT/voucher; janela 72h; taxa 2%; modo doação (valor 0). |
| **`CouponRedeemer`** | `AccessControl` | Cupom/convite on-chain (`maxUses`/`expiry`/resgate único) → mint RVM. |
| `Canonical/IEntryPoint.sol` | Interface (stub) | Stub leve do EntryPoint ERC-4337 (p/ compilação isolada). |
| `Canonical/ISafe.sol` | Interface + `SafeMock` | Stub leve da Safe + módulo 4337 (p/ compilação isolada). |

> Os canônicos (EntryPoint, Safe+4337, Coinbase `webauthn-solidity`, Paymaster) **não são reimplementados**
> aqui. São stubs/mocks para o revoa compilar/testar isoladamente. Em produção, **plugar os auditados**
> (ver §6 e ADR-0002).

---

## 2. Funções públicas-chave

### 2.1 RVM (`src/RVM.sol`)
- `constructor(address admin, uint256 initialSupply)` — nome `Revoa Credit` (`RVM`), 18 decimais; `admin` recebe `DEFAULT_ADMIN` + `MINTER` + `BURNER` (bootstrap).
- `mint(address to, uint256 amount)` — `onlyRole(MINTER_ROLE)`.
- `burn(address from, uint256 amount)` — `onlyRole(BURNER_ROLE)`.
- Eventos: `Minted`, `Burned`. (Transfer/Approval herdados do ERC-20.)

### 2.2 Treasury (`src/Treasury.sol`) — Fundo Comunitário
- `constructor(address admin)`.
- `receiveFee() payable` / `receive()` — recebimento de ETH (reserva).
- `notifyFee(address from, uint256 amount)` — hook de auditoria/Indexer para taxa em RVM.
- `withdraw(address token, address to, uint256 amount)` — `onlyRole(FUNDS_MANAGER_ROLE)`; `token=address(0)` = ETH. **Só para custeio de infra** (transparência em `revoa.org`).
- Eventos: `FundsReceived`, `FundsWithdrawn`.

### 2.3 ProductNFT (`src/ProductNFT.sol`)
- `constructor(address admin)`.
- `mintToEscrow(address escrow, uint256 listingId, string tokenURI)` — `onlyRole(MINTER_ROLE)`; minta o NFT **direto para o `escrow`** (mint-to-escrow, ADR-0005); 1 NFT por `listingId`.
- Mapeamentos públicos: `tokenToListing(tokenId)`, `listingToToken(listingId)`, `tokenURI(tokenId)`.
- Evento: `MintedToEscrow`. O Vault (dono) transfere via `safeTransferFrom` padrão.

### 2.4 ServiceVoucher (`src/ServiceVoucher.sol`)
- `constructor(address admin)`.
- `mintOnPurchase(address to, uint256 listingId, uint64 expiry)` — `onlyRole(MINTER_ROLE)`; minta 1 voucher ao comprador (mint-on-purchase, ADR-0005).
- `redeem(uint256 tokenId)` — só o dono; `!redeemed`, `!expired`.
- `burn(uint256 tokenId)` — `BURNER_ROLE` (Vault) ou dono.
- `isExpired(tokenId)`, `getVoucher(tokenId)`.
- **Não transferível** (`safeTransferFrom`/`safeBatchTransferFrom` revertem) — voucher é pessoal.
- Eventos: `VoucherMinted`, `VoucherRedeemed`, `VoucherBurned`.

### 2.5 EscrowVault (`src/EscrowVault.sol`)
- `constructor(admin, rvm, treasury(payable), productNft, serviceVoucher)`.
- Constantes: `DISPUTE_WINDOW = 72 hours`, `FEE_BPS = 200` (2%).
- `createTrade(buyer, total, AssetKind{Product,Service}, assetContract, tokenId)` — vendedor cria (Produto exige NFT já no Vault).
- `fundTrade(tradeId)` — comprador bloqueia RVM (`transferFrom`); doação (total=0) não move RVM.
- `release(tradeId)` — **cooperativa** (vendedor/comprador) **ou auto** após 72h por qualquer um. Atomic swap: vendedor `total−2%`, Treasury `2%`, ativo ao comprador.
- `openDispute(tradeId)` — dentro da janela 72h.
- `claimArbitrator(tradeId, bool releaseToSeller)` — `onlyRole(ARBITRATOR_ROLE)`; decide libera ou reembolsa.
- `cancel(tradeId)` — cooperativo (Created/Funded); reembolsa comprador e devolve/queima o ativo.
- `onERC721Received` — permite ao Vault custodiar NFTs (recebe via `_safeMint`).
- Eventos: `TradeCreated`, `TradeFunded`, `TradeReleased`, `TradeCancelled`, `TradeDisputed`, `ArbitratorResolved`.

### 2.6 CouponRedeemer (`src/CouponRedeemer.sol`)
- `constructor(admin, rvm)`.
- `createCoupon(code, amount, maxUses, expiry)` / `revokeCoupon(code)` — `onlyRole(COUPON_ADMIN_ROLE)`.
- `redeem(code)` — valida (exists, !revoked, !expired, maxUses, !usedBy) → `RVM.mint(msg.sender, amount)`.
- Eventos: `CouponCreated`, `CouponRevoked`, `CouponRedeemed`.

---

## 3. Máquinas de estado

### PRODUTO (mint-to-escrow → atomic swap move só RVM)
```
Listar  → ProductNFT.mintToEscrow(Vault)         [NFT nasce no Vault]
createTrade (Created) → fundTrade (Funded)
   ├─ release (Released)      → seller=total−2% · Treasury=2% · NFT→buyer
   ├─ openDispute → claimArbitrator (Released|Refunded)
   └─ cancel (Cancelled)      → RVM→buyer · NFT→seller
Após 72h sem disputa: qualquer um força release (auto).
```

### SERVIÇO (mint-on-purchase → voucher queimado na liberação)
```
mintOnPurchase (voucher→buyer) → createTrade (Created) → fundTrade (Funded)
   ├─ release (Released)       → seller=total−2% · Treasury=2% · voucher burn
   ├─ openDispute → claimArbitrator
   └─ cancel (Cancelled)       → RVM→buyer · voucher burn (invalidado)
Expiry(30d sem redeem) → reembolso (claim via árbitro).
```

### DOAÇÃO / VOLUNTARIADO (valor 0 — sem RVM do receptor, sem taxa)
```
total=0 → createTrade → fundTrade (0 RVM) → release
   Produto:   só transfere o NFT ao receptor (doação).
   Serviço:   só queima o voucher (voluntariado prestado).
Recompensa multi-eixo (reputação + bônus RVM admin + pontos) creditada OFF-CHAIN ao doador/voluntário (ADR-0010).
```

> **Mint-to-escrow ≠ mint-on-purchase:** produto (NFT) ao listar; serviço (voucher) ao comprar. Lifecycles opostos — não misturar (armadilha do AGENTS.md).

---

## 4. Deploy (`contracts/script/Deploy.s.sol`)

Ordem (dependências):
1. `RVM` (admin, initialSupply)
2. `Treasury` (admin)
3. `ProductNFT` (admin)
4. `ServiceVoucher` (admin)
5. `EscrowVault` (admin, rvm, treasury, productNft, serviceVoucher)
6. `CouponRedeemer` (admin, rvm)

Concessão de roles (no script):
- `RVM.MINTER_ROLE` → `CouponRedeemer` (resgate minta RVM) e `EscrowVault` (flexibilidade p/ bônus futuro).
- `ServiceVoucher.BURNER_ROLE` → `EscrowVault` (queima voucher na liberação/cancelamento).
- `EscrowVault.ARBITRATOR_ROLE` → já no `admin` (construtor); em produção, atribuir à multisig/conta de moderação.

Variáveis de ambiente: `DEPLOY_ADMIN` (default: deployer), `RVM_INITIAL_SUPPLY` (default: 0 — mint sob demanda).

```bash
# Exemplo (anvil local via wrapper)
.\scripts\foundry.ps1 forge script script/Deploy.s.sol \
  --rpc-url http://host.docker.internal:8545 --broadcast \
  --private-key <ANVIL_KEY> --root contracts
```

---

## 5. Como rodar (Foundry via wrapper Docker)

> Não há `forge` no host Windows. Use sempre `.\scripts\foundry.ps1` (roda no container
> `ghcr.io/foundry-rs/foundry`, monta o repo em `/workspace`). O `foundry.toml` está em `contracts/`,
> então aponte o forge com `--root contracts`.

```powershell
cd C:\Users\rodne\projetosia\revoa

# Build (compila src/ + test/ + script/)
.\scripts\foundry.ps1 forge build --root contracts

# Testes unitários (todos os estados)
.\scripts\foundry.ps1 forge test --root contracts

# Cobertura (≥90% nos contratos críticos)
.\scripts\foundry.ps1 forge coverage --root contracts

# Nó local (interativo — terminal próprio): anvil em http://127.0.0.1:8545
.\scripts\foundry.ps1 anvil
```

**Configuração (`contracts/foundry.toml`):** `solc = "0.8.24"`, `optimizer = true` (200 runs),
`evm_version = "cancun"` (OZ 5.7 usa `mcopy` — opcode Cancun; a Subnet-EVM suporta), remappings
`@openzeppelin/=lib/openzeppelin-contracts/` e `forge-std/=lib/forge-std/src/`.

> **Nota sobre `evm_version`:** o ROADMAP/ARCHITECTURE citava `paris`, mas OpenZeppelin 5.7 exige
> `mcopy` (Cancun) em `Arrays.sol`/`Bytes.sol`. Usamos `cancun` (compatível com Avalanche Subnet-EVM).
> Para rebaixar a `paris`, seria necessário fixar OZ em versão < 5.7.

---

## 6. Canônicos (Account Abstraction) — plugar depois

Os contratos de Account Abstraction **não foram reimplementados** (YAGNI + segurança). Existem
**stubs/mocks leves** em `contracts/src/Canonical/` apenas para o revoa compilar/testar isoladamente:

- `IEntryPoint.sol` — interface mínima do EntryPoint ERC-4337 (`handleOps`).
- `ISafe.sol` — interface mínima da Safe + módulo 4337, e um `SafeMock` para testes.

**Em produção (Fase 1+, ADR-0002), plugar os canônicos auditados:**
1. **EntryPoint** canônico (`eth-infinitism/account-abstraction`) no endereço fixo da chain.
2. **Safe + módulo 4337** (Safe {Core}) por usuário — com recovery admin+timelock (privado).
3. **Coinbase `webauthn-solidity`** (signer P-256) como signer WebAuthn/passkey dentro de cada Safe.
4. **Verifying Paymaster** (ERC-4337) — patrocina gas (usuário não vê ETH; só RVM).
5. **Bundler Stackup** (off-chain) — encaminha UserOps.

A troca do stub pelo canônico é transparente para `RVM`/`ProductNFT`/`ServiceVoucher`/`EscrowVault`/
`CouponRedeemer`/`Treasury` (eles só dependem de ERC-20/721/1155 padrão). As Safes interagem com o
escrow como qualquer EOA (aprovam RVM, assinam `fundTrade`/`release` via UserOp).

---

## 7. Estado da validação (Marco M2)

- `forge build` — **OK** (apenas lint warnings de `block.timestamp`, intencional — janela 72h por design).
- `forge test` — **67 testes, 0 falhas** (RVM, Treasury, ProductNFT, ServiceVoucher, EscrowVault, CouponRedeemer).
- `forge coverage` (% de linhas nos contratos principais):
  - `CouponRedeemer` 100% · `ProductNFT` 100% · `EscrowVault` 97% · `ServiceVoucher` 95% · `RVM` 89% · `Treasury` 87.5%.
  - `Deploy.s.sol` e `Canonical/ISafe.sol` 0% (script de deploy + stub — sem teste de unidade).

---

## 8. Próximos passos (pós-M2)

- Indexer (eventos → read models idempotentes `txHash+logIndex`) — `.NET` Nethereum.
- Plugar canônicos (EntryPoint/Safe/webauthn/Paymaster) — ADR-0002.
- Demurrage (keeper IPCA-trimestral) e cupom admin CRUD — Fase 3.
- Subnet-EVM config (`chain/`) e deploy em rede local privada.

*Última atualização: Marco M2 (03/08/2026).*
