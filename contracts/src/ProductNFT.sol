// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {ERC721} from "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import {ERC721URIStorage} from "@openzeppelin/contracts/token/ERC721/extensions/ERC721URIStorage.sol";
import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";

/// @title ProductNFT — Produto tokenizado (ERC-721, mint-to-escrow).
/// @notice Ao LISTAR um produto, o MINTER minta o NFT DIRETO para o EscrowVault
/// (o NFT nunca toca a carteira do vendedor → impossível dupla venda durante a oferta).
/// Na liberação do escrow, o Vault transfere o NFT ao comprador; no cancelamento, devolve ao vendedor.
/// Ver ADR-0005.
contract ProductNFT is ERC721URIStorage, AccessControl {
    bytes32 public constant MINTER_ROLE = keccak256("MINTER_ROLE");

    uint256 private _nextId = 1; // token ids começam em 1 (0 = inexistente)

    mapping(uint256 => uint256) public tokenToListing; // tokenId => listingId (rastreabilidade/Indexer)
    mapping(uint256 => uint256) public listingToToken; // listingId => tokenId (1 listing = 1 NFT)

    /// @dev Endereço do EscrowVault (one-shot, imutável após config). Evita mint para outro destino.
    address public escrowVault;
    bool private _escrowVaultSet;

    event MintedToEscrow(
        address indexed escrow, uint256 indexed listingId, uint256 indexed tokenId, string tokenURI
    );
    event EscrowVaultSet(address indexed escrow);

    constructor(address admin) ERC721("Revoa Product", "RVMP") {
        if (admin == address(0)) revert ProductNFT__ZeroAddress();
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(MINTER_ROLE, admin);
    }

    /// @notice Setter one-shot (DEFAULT_ADMIN_ROLE) do endereço do EscrowVault.
    /// Necessário por chicken-and-egg (EscrowVault precisa do ProductNFT no constructor).
    function setEscrowVault(address vault) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (vault == address(0)) revert ProductNFT__ZeroAddress();
        if (_escrowVaultSet) revert ProductNFT__EscrowAlreadySet();
        escrowVault = vault;
        _escrowVaultSet = true;
        emit EscrowVaultSet(vault);
    }

    /// @notice Mint on-chain de um produto DIRETO para o endereço do EscrowVault.
    /// @param escrow DEVE ser o endereço do EscrowVault configurado (one-shot). O NFT nasce lá.
    /// @param listingId Id off-chain do anúncio (anti-dupla-mint via listingToToken).
    /// @param tokenURI_ URI dos metadados (MinIO).
    function mintToEscrow(address escrow, uint256 listingId, string calldata tokenURI_)
        external
        onlyRole(MINTER_ROLE)
        returns (uint256 tokenId)
    {
        if (!_escrowVaultSet) revert ProductNFT__EscrowNotSet();
        if (escrow != escrowVault) revert ProductNFT__BadEscrow();
        if (listingToToken[listingId] != 0) revert ProductNFT__AlreadyMinted(listingId);

        tokenId = _nextId++;
        tokenToListing[tokenId] = listingId;
        listingToToken[listingId] = tokenId;
        _safeMint(escrow, tokenId);
        _setTokenURI(tokenId, tokenURI_);
        emit MintedToEscrow(escrow, listingId, tokenId, tokenURI_);
    }

    /// @dev O EscrowVault (dono do NFT) transfere via safeTransferFrom padrão do ERC-721.
    /// Não há função customizada de transfer — o dono (Vault) move livremente.
    /// tokenURI é fornecido por ERC721URIStorage (não há override necessário).

    function supportsInterface(bytes4 interfaceId)
        public
        view
        virtual
        override(ERC721URIStorage, AccessControl)
        returns (bool)
    {
        return super.supportsInterface(interfaceId);
    }

    error ProductNFT__ZeroAddress();
    error ProductNFT__AlreadyMinted(uint256 listingId);
    error ProductNFT__EscrowAlreadySet();
    error ProductNFT__EscrowNotSet();
    error ProductNFT__BadEscrow();
}
