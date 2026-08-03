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

    function testRevert_Mint_ZeroEscrow() public {
        vm.expectRevert(ProductNFT.ProductNFT__ZeroAddress.selector);
        nft.mintToEscrow(address(0), 1, "uri");
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
