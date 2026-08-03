// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {RVM} from "../src/RVM.sol";
import {Treasury} from "../src/Treasury.sol";

contract TreasuryTest is Test {
    RVM rvm;
    Treasury treasury;
    address admin = address(this);
    address operator = address(0x0AB);
    address recipient = address(0x8ECE);

    function setUp() public {
        rvm = new RVM(admin, 1_000_000e18);
        treasury = new Treasury(admin);
        treasury.grantRole(treasury.FUNDS_MANAGER_ROLE(), operator);
        // Fundo Comunitário recebe taxas em RVM (simula saída do EscrowVault).
        rvm.transfer(address(treasury), 5_000e18);
        // Fundo com ETH p/ teste.
        (bool ok,) = address(treasury).call{value: 2 ether}("");
        require(ok, "eth funding failed");
    }

    function test_Balances() public view {
        assertEq(rvm.balanceOf(address(treasury)), 5_000e18);
        assertEq(address(treasury).balance, 2 ether);
    }

    function test_Withdraw_RVM_ByOperator() public {
        vm.prank(operator);
        treasury.withdraw(address(rvm), recipient, 1_000e18);
        assertEq(rvm.balanceOf(recipient), 1_000e18);
        assertEq(rvm.balanceOf(address(treasury)), 4_000e18);
    }

    function test_Withdraw_ETH_ByAdmin() public {
        uint256 before = recipient.balance;
        treasury.withdraw(address(0), recipient, 1 ether);
        assertEq(recipient.balance - before, 1 ether);
    }

    function test_NotifyFee_Emits() public {
        vm.expectEmit(true, false, false, true);
        emit Treasury.FundsReceived(admin, 250e18);
        treasury.notifyFee(admin, 250e18);
    }

    function test_Receive_ETH_Emits() public {
        vm.expectEmit(true, false, false, true);
        emit Treasury.FundsReceived(admin, 0.5 ether);
        (bool ok,) = address(treasury).call{value: 0.5 ether}("");
        require(ok);
    }

    function testRevert_Withdraw_Unauthorized() public {
        address nobody = address(0xBAAD);
        vm.prank(nobody);
        vm.expectRevert();
        treasury.withdraw(address(rvm), recipient, 100e18);
    }

    function testRevert_Withdraw_ZeroAmount() public {
        vm.expectRevert(Treasury.Treasury__ZeroAmount.selector);
        treasury.withdraw(address(rvm), recipient, 0);
    }

    function testRevert_Withdraw_ZeroAddress() public {
        vm.expectRevert(Treasury.Treasury__ZeroAddress.selector);
        treasury.withdraw(address(rvm), address(0), 100e18);
    }

    function testRevert_Withdraw_InsufficientBalance() public {
        vm.expectRevert(Treasury.Treasury__InsufficientBalance.selector);
        treasury.withdraw(address(rvm), recipient, 99_999_999e18);
    }

    function testRevert_Withdraw_ETH_Insufficient() public {
        vm.expectRevert(Treasury.Treasury__InsufficientBalance.selector);
        treasury.withdraw(address(0), recipient, 99 ether);
    }

    receive() external payable {}
}
