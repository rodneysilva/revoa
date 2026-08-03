// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {RVM} from "../src/RVM.sol";
import {CouponRedeemer} from "../src/CouponRedeemer.sol";

contract CouponRedeemerTest is Test {
    RVM rvm;
    CouponRedeemer redeemer;
    address admin = address(this);
    address alice = address(0xA11CE);
    address bob = address(0xB0B);
    address carol = address(0xCA401);
    address dave = address(0xDA7E);

    string constant CODE = "WELCOME2026";

    function setUp() public {
        rvm = new RVM(admin, 0);
        redeemer = new CouponRedeemer(admin, address(rvm));
        rvm.grantRole(rvm.MINTER_ROLE(), address(redeemer));
    }

    function test_CreateCoupon() public {
        redeemer.createCoupon(CODE, 100e18, 3, uint64(block.timestamp + 7 days));
        // leitura pública do mapping: (amount, maxUses, usedCount, expiry, exists, revoked)
        (uint256 amount, uint32 maxUses, uint32 used,,,) = redeemer.coupons(keccak256(bytes(CODE)));
        assertEq(amount, 100e18);
        assertEq(maxUses, 3);
        assertEq(used, 0);
    }

    function test_Redeem_MintsRVM() public {
        redeemer.createCoupon(CODE, 100e18, 3, 0);
        vm.prank(alice);
        redeemer.redeem(CODE);
        assertEq(rvm.balanceOf(alice), 100e18);
        assertTrue(redeemer.usedBy(keccak256(bytes(CODE)), alice));
    }

    function testRevert_Redeem_TwiceSameWallet() public {
        redeemer.createCoupon(CODE, 100e18, 3, 0);
        vm.startPrank(alice);
        redeemer.redeem(CODE);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__AlreadyUsed.selector);
        redeemer.redeem(CODE);
        vm.stopPrank();
    }

    function test_MaxUses_Reached() public {
        redeemer.createCoupon(CODE, 50e18, 3, 0);
        vm.prank(alice);
        redeemer.redeem(CODE);
        vm.prank(bob);
        redeemer.redeem(CODE);
        vm.prank(carol);
        redeemer.redeem(CODE);
        // 4º estoura maxUses
        vm.prank(dave);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__MaxUsesReached.selector);
        redeemer.redeem(CODE);
    }

    function test_DistinctWalletsEachRedeem() public {
        redeemer.createCoupon(CODE, 50e18, 2, 0);
        vm.prank(alice);
        redeemer.redeem(CODE);
        vm.prank(bob);
        redeemer.redeem(CODE);
        assertEq(rvm.balanceOf(alice), 50e18);
        assertEq(rvm.balanceOf(bob), 50e18);
    }

    function testRevert_Redeem_Expired() public {
        uint64 exp = uint64(block.timestamp + 1 hours);
        redeemer.createCoupon(CODE, 100e18, 5, exp);
        vm.warp(block.timestamp + 2 hours);
        vm.prank(alice);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__Expired.selector);
        redeemer.redeem(CODE);
    }

    function testRevert_Redeem_Revoked() public {
        redeemer.createCoupon(CODE, 100e18, 5, 0);
        redeemer.revokeCoupon(CODE);
        vm.prank(alice);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__Revoked.selector);
        redeemer.redeem(CODE);
    }

    function testRevert_Redeem_NotFound() public {
        vm.prank(alice);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__NotFound.selector);
        redeemer.redeem("NOPE");
    }

    function testRevert_Create_InvalidExpiry() public {
        vm.warp(1_000_000);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__InvalidExpiry.selector);
        redeemer.createCoupon(CODE, 100e18, 3, uint64(block.timestamp - 1));
    }

    function testRevert_Create_AlreadyExists() public {
        redeemer.createCoupon(CODE, 100e18, 3, 0);
        vm.expectRevert(CouponRedeemer.CouponRedeemer__AlreadyExists.selector);
        redeemer.createCoupon(CODE, 200e18, 5, 0);
    }

    function testRevert_Create_Unauthorized() public {
        vm.prank(alice);
        vm.expectRevert();
        redeemer.createCoupon(CODE, 100e18, 3, 0);
    }
}
