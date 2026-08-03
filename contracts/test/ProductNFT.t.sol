// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {ProductNFT} from "../src/ProductNFT.sol";

contract ProductNFTTest is Test {
    ProductNFT nft;
    address admin = address(this);
    address escrow = address(0xE5C70);
    address seller = address(0x5E11);

    function setUp() public {
        nft = new ProductNFT(admin);
        // W1: amarra o escrow (one-shot). Sem isso, mintToEscrow reverte.
        nft.setEscrowVault(escrow);
    }

    function test_MintToEscrow_OwnerIsEscrow() public {
        uint256 id = nft.mintToEscrow(escrow, 1, "ipfs://meta/1");
        assertEq(nft.ownerOf(id), escrow, "NFT deve nascer no escrow, nunca na carteira do vendedor");
        assertEq(nft.tokenToListing(id), 1);
        assertEq(nft.listingToToken(1), id);
        assertEq(nft.tokenURI(id), "ipfs://meta/1");
    }

    function test_MintToEscrow_IncrementsId() public {
        uint256 id1 = nft.mintToEscrow(escrow, 1, "uri1");
        uint256 id2 = nft.mintToEscrow(escrow, 2, "uri2");
        assertEq(id1, 1);
        assertEq(id2, 2);
    }

    function testRevert_Mint_NotMinter() public {
        vm.prank(seller);
        vm.expectRevert();
        nft.mintToEscrow(escrow, 1, "uri");
    }

    function testRevert_Mint_DuplicateListing() public {
        nft.mintToEscrow(escrow, 1, "uri");
        vm.expectRevert(abi.encodeWithSelector(ProductNFT.ProductNFT__AlreadyMinted.selector, 1));
        nft.mintToEscrow(escrow, 1, "uri2");
    }

    function testRevert_Mint_BadEscrow() public {
        // W1: escrow != escrowVault configurado → rejeita (mesmo se não-zero).
        vm.expectRevert(ProductNFT.ProductNFT__BadEscrow.selector);
        nft.mintToEscrow(address(0xBAD), 1, "uri");
    }

    function testRevert_SetEscrow_Twice() public {
        // W1: setter é one-shot.
        vm.expectRevert(ProductNFT.ProductNFT__EscrowAlreadySet.selector);
        nft.setEscrowVault(escrow);
    }

    function testRevert_Mint_ZeroEscrow_NotSet() public {
        // Num contrato sem escrow configurado, mint reverte com EscrowNotSet.
        ProductNFT fresh = new ProductNFT(admin);
        vm.expectRevert(ProductNFT.ProductNFT__EscrowNotSet.selector);
        fresh.mintToEscrow(escrow, 1, "uri");
    }

    function test_EscrowCanTransferItsNFT() public {
        // O escrow (dono) transfere o NFT a um comprador simulando release.
        uint256 id = nft.mintToEscrow(escrow, 1, "uri");
        address buyer = address(0xB0B);
        vm.prank(escrow);
        nft.safeTransferFrom(escrow, buyer, id);
        assertEq(nft.ownerOf(id), buyer);
    }

    function testRevert_SellerCannotTransferNotOwned() public {
        uint256 id = nft.mintToEscrow(escrow, 1, "uri");
        // seller NÃO é dono (NFT está no escrow)
        vm.prank(seller);
        vm.expectRevert();
        nft.safeTransferFrom(seller, address(0xB0B), id);
    }

    function test_SupportsInterfaces() public view {
        assertTrue(nft.supportsInterface(0x80ac58cd)); // ERC-721
        assertTrue(nft.supportsInterface(0x5b5e139f)); // ERC-721 Metadata
    }
}
