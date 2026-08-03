// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {ServiceVoucher} from "../src/ServiceVoucher.sol";

contract ServiceVoucherTest is Test {
    ServiceVoucher voucher;
    address admin = address(this);
    address vault = address(0xAAA1);
    address buyer = address(0xB0B);
    address other = address(0x0B8B);

    function setUp() public {
        voucher = new ServiceVoucher(admin);
        voucher.grantRole(voucher.BURNER_ROLE(), vault);
    }

    function test_MintOnPurchase() public {
        uint64 exp = uint64(block.timestamp + 30 days);
        uint256 id = voucher.mintOnPurchase(buyer, 42, exp);
        assertEq(voucher.balanceOf(buyer, id), 1);
        ServiceVoucher.Voucher memory v = voucher.getVoucher(id);
        assertEq(v.expiry, exp);
        assertEq(v.owner, buyer);
        assertFalse(v.redeemed);
        assertEq(v.listingId, 42);
    }

    function test_Redeem_MarksUsed() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 30 days));
        vm.prank(buyer);
        voucher.redeem(id);
        assertTrue(voucher.getVoucher(id).redeemed);
    }

    function testRevert_Redeem_NotOwner() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 30 days));
        vm.prank(other);
        vm.expectRevert(ServiceVoucher.ServiceVoucher__NotOwner.selector);
        voucher.redeem(id);
    }

    function testRevert_Redeem_Twice() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 30 days));
        vm.startPrank(buyer);
        voucher.redeem(id);
        vm.expectRevert(ServiceVoucher.ServiceVoucher__AlreadyRedeemed.selector);
        voucher.redeem(id);
        vm.stopPrank();
    }

    function testRevert_Redeem_Expired() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 1 days));
        vm.warp(block.timestamp + 2 days);
        vm.prank(buyer);
        vm.expectRevert(ServiceVoucher.ServiceVoucher__Expired.selector);
        voucher.redeem(id);
    }

    function test_IsExpired() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 1 days));
        assertFalse(voucher.isExpired(id));
        vm.warp(block.timestamp + 2 days);
        assertTrue(voucher.isExpired(id));
    }

    function test_Burn_ByBurnerRole() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, uint64(block.timestamp + 30 days));
        vm.prank(vault);
        voucher.burn(id);
        assertEq(voucher.balanceOf(buyer, id), 0);
    }

    function test_Burn_ByOwner() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, 0);
        vm.prank(buyer);
        voucher.burn(id);
        assertEq(voucher.balanceOf(buyer, id), 0);
    }

    function testRevert_Burn_Unauthorized() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, 0);
        vm.prank(other);
        vm.expectRevert(ServiceVoucher.ServiceVoucher__NotAuthorized.selector);
        voucher.burn(id);
    }

    function testRevert_Mint_NotMinter() public {
        vm.prank(other);
        vm.expectRevert();
        voucher.mintOnPurchase(buyer, 42, 0);
    }

    function testRevert_Transfer_NonTransferable() public {
        uint256 id = voucher.mintOnPurchase(buyer, 42, 0);
        vm.prank(buyer);
        vm.expectRevert(ServiceVoucher.ServiceVoucher__NonTransferable.selector);
        voucher.safeTransferFrom(buyer, other, id, 1, "");
    }

    function test_SupportsInterfaces() public view {
        assertTrue(voucher.supportsInterface(0xd9b67a26)); // ERC-1155
    }
}
