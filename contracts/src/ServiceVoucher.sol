// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {ERC1155} from "@openzeppelin/contracts/token/ERC1155/ERC1155.sol";
import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";

/// @title ServiceVoucher — Voucher de serviço (ERC-1155, mint-on-purchase).
/// @notice Mintado APENAS na COMPRA (mint-on-purchase) → carteira do comprador.
/// `redeem(id)` marca usado (confirmação); `burn(id)` queima (liberação/voluntariado prestado).
/// Validade configurável (default 30d); expirado → auto-reembolso off-chain + claim via árbitro.
/// Não transferível (voucher é pessoal). Ver ADR-0005.
contract ServiceVoucher is ERC1155, AccessControl {
    bytes32 public constant MINTER_ROLE = keccak256("MINTER_ROLE");
    bytes32 public constant BURNER_ROLE = keccak256("BURNER_ROLE");

    struct Voucher {
        uint64 expiry; // timestamp limite (block.timestamp); 0 = sem expiração
        address owner; // comprador original (voucher é pessoal/não-transferível)
        bool redeemed; // marcado quando o serviço foi prestado/confirmado
        uint256 listingId; // rastreabilidade
    }

    uint256 private _nextId = 1; // ids começam em 1 (0 = inexistente)
    mapping(uint256 => Voucher) private _vouchers;

    event VoucherMinted(
        address indexed to, uint256 indexed listingId, uint256 indexed tokenId, uint64 expiry
    );
    event VoucherRedeemed(uint256 indexed tokenId, address indexed owner);
    event VoucherBurned(uint256 indexed tokenId, address indexed owner);

    constructor(address admin) ERC1155("") {
        if (admin == address(0)) revert ServiceVoucher__ZeroAddress();
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(MINTER_ROLE, admin);
        _grantRole(BURNER_ROLE, admin);
    }

    /// @notice Mint-on-purchase: cunha 1 voucher para o comprador com expiração.
    function mintOnPurchase(address to, uint256 listingId, uint64 expiry)
        external
        onlyRole(MINTER_ROLE)
        returns (uint256 tokenId)
    {
        if (to == address(0)) revert ServiceVoucher__ZeroAddress();
        tokenId = _nextId++;
        _vouchers[tokenId] = Voucher({expiry: expiry, owner: to, redeemed: false, listingId: listingId});
        _mint(to, tokenId, 1, "");
        emit VoucherMinted(to, listingId, tokenId, expiry);
    }

    /// @notice Marca o voucher como usado (confirmação do comprador). Só o dono, não expirado, não usado.
    function redeem(uint256 tokenId) external {
        Voucher storage v = _vouchers[tokenId];
        if (v.owner == address(0)) revert ServiceVoucher__NotFound(tokenId);
        if (v.owner != msg.sender) revert ServiceVoucher__NotOwner();
        if (v.redeemed) revert ServiceVoucher__AlreadyRedeemed();
        if (v.expiry != 0 && block.timestamp > v.expiry) revert ServiceVoucher__Expired();
        v.redeemed = true;
        emit VoucherRedeemed(tokenId, msg.sender);
    }

    /// @notice Queima o voucher (liberação do escrow / voluntariado prestado).
    /// Chamado pelo BURNER_ROLE (EscrowVault) ou pelo próprio dono.
    function burn(uint256 tokenId) external {
        Voucher storage v = _vouchers[tokenId];
        if (v.owner == address(0)) revert ServiceVoucher__NotFound(tokenId);
        bool isBurner = hasRole(BURNER_ROLE, msg.sender);
        if (!isBurner && v.owner != msg.sender) revert ServiceVoucher__NotAuthorized();
        _burn(v.owner, tokenId, 1);
        emit VoucherBurned(tokenId, v.owner);
        delete _vouchers[tokenId];
    }

    function isExpired(uint256 tokenId) external view returns (bool) {
        Voucher storage v = _vouchers[tokenId];
        if (v.owner == address(0)) revert ServiceVoucher__NotFound(tokenId);
        return v.expiry != 0 && block.timestamp > v.expiry;
    }

    function getVoucher(uint256 tokenId) external view returns (Voucher memory) {
        if (_vouchers[tokenId].owner == address(0)) revert ServiceVoucher__NotFound(tokenId);
        return _vouchers[tokenId];
    }

    function supportsInterface(bytes4 interfaceId)
        public
        view
        virtual
        override(ERC1155, AccessControl)
        returns (bool)
    {
        return super.supportsInterface(interfaceId);
    }

    /// @dev Voucher é pessoal: bloqueia transferências externas (mantém `owner` estável p/ burn).
    function safeTransferFrom(address, address, uint256, uint256, bytes memory)
        public
        pure
        override
    {
        revert ServiceVoucher__NonTransferable();
    }

    function safeBatchTransferFrom(address, address, uint256[] memory, uint256[] memory, bytes memory)
        public
        pure
        override
    {
        revert ServiceVoucher__NonTransferable();
    }

    error ServiceVoucher__ZeroAddress();
    error ServiceVoucher__NotFound(uint256 tokenId);
    error ServiceVoucher__NotOwner();
    error ServiceVoucher__NotAuthorized();
    error ServiceVoucher__AlreadyRedeemed();
    error ServiceVoucher__Expired();
    error ServiceVoucher__NonTransferable();
}
