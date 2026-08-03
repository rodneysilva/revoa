// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {RVM} from "../src/RVM.sol";

contract RVMTest is Test {
    RVM rvm;
    address admin = address(this);
    address minter = address(0x1111);
    address burner = address(0x2222);
    address alice = address(0xA11CE);
    address bob = address(0xB0B);

    function setUp() public {
        rvm = new RVM(admin, 1_000_000e18);
        rvm.grantRole(rvm.MINTER_ROLE(), minter);
        rvm.grantRole(rvm.BURNER_ROLE(), burner);
    }

    function test_Deploy_MetadataAndSupply() public view {
        assertEq(rvm.name(), "Revoa Credit");
        assertEq(rvm.symbol(), "RVM");
        assertEq(rvm.decimals(), 18);
        assertEq(rvm.totalSupply(), 1_000_000e18);
        assertEq(rvm.balanceOf(admin), 1_000_000e18);
    }

    function test_Mint_ByMinter() public {
        vm.prank(minter);
        rvm.mint(alice, 500e18);
        assertEq(rvm.balanceOf(alice), 500e18);
        assertEq(rvm.totalSupply(), 1_000_000e18 + 500e18);
    }

    function testRevert_Mint_Unauthorized() public {
        vm.prank(bob);
        vm.expectRevert();
        rvm.mint(alice, 100e18);
    }

    function testRevert_Mint_ZeroAddress() public {
        vm.prank(minter);
        vm.expectRevert(RVM.RVM__ZeroAddress.selector);
        rvm.mint(address(0), 100e18);
    }

    function test_Burn_ByBurner() public {
        // admin transfere para bob, burner queima
        rvm.transfer(bob, 300e18);
        vm.prank(burner);
        rvm.burn(bob, 100e18);
        assertEq(rvm.balanceOf(bob), 200e18);
        assertEq(rvm.totalSupply(), 1_000_000e18 - 100e18);
    }

    function testRevert_Burn_Unauthorized() public {
        rvm.transfer(bob, 300e18);
        vm.prank(bob);
        vm.expectRevert();
        rvm.burn(bob, 100e18); // bob não é burner
    }

    function testRevert_Burn_InsufficientBalance() public {
        vm.prank(burner);
        vm.expectRevert(); // _burn reverte (saldo insuficiente)
        rvm.burn(alice, 1e18);
    }

    function test_Roles() public view {
        assertTrue(rvm.hasRole(rvm.DEFAULT_ADMIN_ROLE(), admin));
        assertTrue(rvm.hasRole(rvm.MINTER_ROLE(), minter));
        assertTrue(rvm.hasRole(rvm.BURNER_ROLE(), burner));
        assertFalse(rvm.hasRole(rvm.MINTER_ROLE(), bob));
    }

    function test_Transfer_Standard() public {
        rvm.transfer(alice, 250e18);
        assertEq(rvm.balanceOf(alice), 250e18);
    }
}
