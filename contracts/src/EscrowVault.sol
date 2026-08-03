// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import {IERC721Receiver} from "@openzeppelin/contracts/token/ERC721/IERC721Receiver.sol";

import {RVM} from "./RVM.sol";
import {Treasury} from "./Treasury.sol";
import {ProductNFT} from "./ProductNFT.sol";
import {ServiceVoucher} from "./ServiceVoucher.sol";

/// @title EscrowVault — Atomic swap de RVM ↔ produto(NFT)/serviço(voucher).
/// @notice Custódia on-chain. Fluxo:
///   createTrade (Created) → fundTrade (Funded) → release (Released) | openDispute→claimArbitrator | cancel.
/// - Janela de disputa 72h (block.timestamp). Pós-janela, qualquer um pode forçar release (auto).
/// - Taxa 2% (200 bps) do vendedor → Treasury (Fundo Comunitário, sem FLP).
/// - Doação/voluntariado: total=0 → só transfere o ativo (NFT ao receptor / burn voucher), SEM taxa.
/// Máquinas de estado: produto (mint-to-escrow), serviço (mint-on-purchase), doação (valor 0). Ver BUSINESS_RULES §2, ADR-0005/0009/0010.
contract EscrowVault is AccessControl, ReentrancyGuard, IERC721Receiver {
    bytes32 public constant ARBITRATOR_ROLE = keccak256("ARBITRATOR_ROLE");

    uint64 public constant DISPUTE_WINDOW = 72 hours;
    uint256 public constant FEE_BPS = 200; // 2%

    RVM public immutable rvm;
    Treasury public immutable treasury;
    ProductNFT public immutable productNft;
    ServiceVoucher public immutable serviceVoucher;

    enum AssetKind {
        Product, // ERC-721 (mint-to-escrow)
        Service // ERC-1155 voucher (mint-on-purchase)
    }
    enum State {
        Created,
        Funded,
        Released,
        Cancelled,
        Disputed,
        Refunded
    }

    struct Trade {
        address seller;
        address buyer;
        uint256 total; // RVM; 0 = doação/voluntariado
        AssetKind kind;
        address assetContract;
        uint256 tokenId; // NFT id (Product) | voucher id (Service)
        State state;
        uint64 fundedAt;
    }

    uint256 private _nextTradeId = 1;
    mapping(uint256 => Trade) public trades;

    event TradeCreated(
        uint256 indexed tradeId,
        address indexed seller,
        address indexed buyer,
        uint256 total,
        AssetKind kind,
        address assetContract,
        uint256 tokenId
    );
    event TradeFunded(uint256 indexed tradeId, uint256 amount);
    event TradeReleased(uint256 indexed tradeId, uint256 sellerGets, uint256 fee);
    event TradeCancelled(uint256 indexed tradeId);
    event TradeDisputed(uint256 indexed tradeId, address indexed by);
    event ArbitratorResolved(uint256 indexed tradeId, bool releaseToSeller);

    constructor(
        address admin,
        address rvmAddr,
        address payable treasuryAddr,
        address productNftAddr,
        address serviceVoucherAddr
    ) {
        if (
            admin == address(0) || rvmAddr == address(0) || treasuryAddr == address(0)
                || productNftAddr == address(0) || serviceVoucherAddr == address(0)
        ) {
            revert EscrowVault__ZeroAddress();
        }
        rvm = RVM(rvmAddr);
        treasury = Treasury(treasuryAddr);
        productNft = ProductNFT(productNftAddr);
        serviceVoucher = ServiceVoucher(serviceVoucherAddr);
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(ARBITRATOR_ROLE, admin);
    }

    /// @notice Cria uma troca (vendedor = msg.sender). Para produto, o NFT já deve estar no Vault (mint-to-escrow).
    function createTrade(
        address buyer,
        uint256 total,
        AssetKind kind,
        address assetContract,
        uint256 tokenId
    ) external returns (uint256 tradeId) {
        if (buyer == address(0)) revert EscrowVault__ZeroAddress();
        if (assetContract == address(0)) revert EscrowVault__ZeroAddress();
        if (kind == AssetKind.Product) {
            // NFT deve estar sob custódia do Vault (mint-to-escrow).
            if (assetContract != address(productNft)) revert EscrowVault__BadAsset();
            if (ProductNFT(assetContract).ownerOf(tokenId) != address(this)) {
                revert EscrowVault__AssetNotInVault();
            }
        } else {
            // Serviço: voucher já mintado ao comprador (mint-on-purchase). Valida existência.
            if (assetContract != address(serviceVoucher)) revert EscrowVault__BadAsset();
            // Checagem leve: o voucher existe (não reverte se inexistente).
            try ServiceVoucher(assetContract).getVoucher(tokenId) {} catch {
                revert EscrowVault__AssetNotInVault();
            }
        }

        tradeId = _nextTradeId++;
        trades[tradeId] = Trade({
            seller: msg.sender,
            buyer: buyer,
            total: total,
            kind: kind,
            assetContract: assetContract,
            tokenId: tokenId,
            state: State.Created,
            fundedAt: 0
        });
        emit TradeCreated(tradeId, msg.sender, buyer, total, kind, assetContract, tokenId);
    }

    /// @notice Comprador bloqueia o RVM no Vault. Doação (total=0) não move RVM.
    function fundTrade(uint256 tradeId) external nonReentrant {
        Trade storage t = trades[tradeId];
        if (t.buyer == address(0)) revert EscrowVault__NotFound();
        if (t.state != State.Created) revert EscrowVault__BadState();
        if (msg.sender != t.buyer) revert EscrowVault__NotBuyer();

        if (t.total > 0) {
            _safeTransferFrom(msg.sender, address(this), t.total);
        }
        t.state = State.Funded;
        t.fundedAt = uint64(block.timestamp);
        emit TradeFunded(tradeId, t.total);
    }

    /// @notice Liberação cooperativa (vendedor OU comprador) ou automática pós-janela de 72h.
    function release(uint256 tradeId) external nonReentrant {
        Trade storage t = trades[tradeId];
        if (t.state != State.Funded) revert EscrowVault__BadState();

        bool isParty = (msg.sender == t.seller || msg.sender == t.buyer);
        bool windowPassed = block.timestamp >= t.fundedAt + DISPUTE_WINDOW;
        if (!isParty && !windowPassed) revert EscrowVault__NotAuthorized();

        _settle(t, true);
        t.state = State.Released;
        emit TradeReleased(tradeId, _sellerGets(t.total), _fee(t.total));
    }

    /// @notice Abre disputa dentro da janela de 72h (vendedor OU comprador).
    function openDispute(uint256 tradeId) external {
        Trade storage t = trades[tradeId];
        if (t.state != State.Funded) revert EscrowVault__BadState();
        if (msg.sender != t.seller && msg.sender != t.buyer) revert EscrowVault__NotAuthorized();
        if (block.timestamp >= t.fundedAt + DISPUTE_WINDOW) revert EscrowVault__WindowExpired();

        t.state = State.Disputed;
        emit TradeDisputed(tradeId, msg.sender);
    }

    /// @notice Árbitro resolve disputa pós-janela. releaseToSeller=true → libera (pagamento+ativo);
    /// false → reembolsa comprador e devolve/queima o ativo.
    function claimArbitrator(uint256 tradeId, bool releaseToSeller)
        external
        onlyRole(ARBITRATOR_ROLE)
        nonReentrant
    {
        Trade storage t = trades[tradeId];
        if (t.state != State.Disputed) revert EscrowVault__BadState();

        _settle(t, releaseToSeller);
        t.state = releaseToSeller ? State.Released : State.Refunded;
        emit ArbitratorResolved(tradeId, releaseToSeller);
    }

    /// @notice Cancelamento cooperativo (antes da entrega). Estado Created ou Funded.
    /// Reembolsa o comprador (se funded) e devolve/queima o ativo ao vendedor.
    function cancel(uint256 tradeId) external nonReentrant {
        Trade storage t = trades[tradeId];
        if (t.state != State.Created && t.state != State.Funded) revert EscrowVault__BadState();
        if (msg.sender != t.seller && msg.sender != t.buyer) revert EscrowVault__NotAuthorized();

        // Reembolso: se financiado com RVM, devolve ao comprador.
        if (t.state == State.Funded && t.total > 0) {
            _safeTransfer(t.buyer, t.total);
        }
        // Ativo de volta ao vendedor: produto → devolve NFT; serviço → queima voucher (invalidado).
        _returnAssetToSeller(t);
        t.state = State.Cancelled;
        emit TradeCancelled(tradeId);
    }

    function getTrade(uint256 tradeId) external view returns (Trade memory) {
        if (trades[tradeId].buyer == address(0)) revert EscrowVault__NotFound();
        return trades[tradeId];
    }

    /// @dev Permite ao Vault receber NFTs de produto (mint-to-escrow via _safeMint). Custódia passiva.
    function onERC721Received(address, address, uint256, bytes calldata)
        external
        pure
        override
        returns (bytes4)
    {
        return IERC721Receiver.onERC721Received.selector;
    }

    // ─────────── Internals ───────────

    /// @dev Liquidação atômica. doRelease=true → paga vendedor (−2%) + Treasury + ativo ao comprador.
    /// doRelease=false → reembolsa comprador + devolve/queima ativo ao vendedor.
    function _settle(Trade storage t, bool doRelease) internal {
        if (doRelease) {
            if (t.total > 0) {
                uint256 fee = _fee(t.total);
                uint256 sellerGets = _sellerGets(t.total);
                _safeTransfer(t.seller, sellerGets);
                _safeTransfer(address(treasury), fee);
            }
            // Ativo ao comprador: produto → transfere NFT; serviço → queima voucher.
            _transferAssetToBuyer(t);
        } else {
            // Reembolso
            if (t.total > 0) {
                _safeTransfer(t.buyer, t.total);
            }
            _returnAssetToSeller(t);
        }
    }

    function _transferAssetToBuyer(Trade storage t) internal {
        if (t.kind == AssetKind.Product) {
            ProductNFT(t.assetContract).safeTransferFrom(address(this), t.buyer, t.tokenId);
        } else {
            // Serviço: voucher já está com o comprador → queima (serviço consumido na liberação).
            ServiceVoucher(t.assetContract).burn(t.tokenId);
        }
    }

    function _returnAssetToSeller(Trade storage t) internal {
        if (t.kind == AssetKind.Product) {
            // Devolve o NFT ao vendedor (ainda sob custódia do Vault).
            ProductNFT(t.assetContract).safeTransferFrom(address(this), t.seller, t.tokenId);
        } else {
            // Serviço cancelado: voucher invalidado (queimado).
            ServiceVoucher(t.assetContract).burn(t.tokenId);
        }
    }

    function _fee(uint256 total) internal pure returns (uint256) {
        return (total * FEE_BPS) / 10_000;
    }

    function _sellerGets(uint256 total) internal pure returns (uint256) {
        return total - _fee(total);
    }

    function _safeTransfer(address to, uint256 amount) internal {
        bool ok = IERC20(address(rvm)).transfer(to, amount);
        if (!ok) revert EscrowVault__TransferFailed();
    }

    function _safeTransferFrom(address from, address to, uint256 amount) internal {
        bool ok = IERC20(address(rvm)).transferFrom(from, to, amount);
        if (!ok) revert EscrowVault__TransferFailed();
    }

    error EscrowVault__ZeroAddress();
    error EscrowVault__NotFound();
    error EscrowVault__BadState();
    error EscrowVault__NotBuyer();
    error EscrowVault__NotAuthorized();
    error EscrowVault__BadAsset();
    error EscrowVault__AssetNotInVault();
    error EscrowVault__WindowExpired();
    error EscrowVault__TransferFailed();
}
