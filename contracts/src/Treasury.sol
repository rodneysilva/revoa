// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";

/// @title Treasury — Fundo Comunitário (sem fins lucrativos).
/// @notice Recebe a taxa de 2% das trocas finalizadas. Reinvestido na operação (infra).
/// Não distribui lucros; `withdraw` só admin para custeio documentado (transparência em revoa.org).
contract Treasury is AccessControl {
    bytes32 public constant FUNDS_MANAGER_ROLE = keccak256("FUNDS_MANAGER_ROLE");

    event FundsReceived(address indexed from, uint256 amount);
    event FundsWithdrawn(address indexed to, address indexed token, uint256 amount);

    constructor(address admin) {
        if (admin == address(0)) revert Treasury__ZeroAddress();
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(FUNDS_MANAGER_ROLE, admin);
    }

    /// @notice Recebe taxa em RVM (transfer direto via EscrowVault.transfer). Emissão de evento p/ Indexer.
    function receiveFee() external payable {
        // Aceita ETH nativo (reserva; raro na subnet). A taxa principal chega via transfer de RVM.
        if (msg.value > 0) {
            emit FundsReceived(msg.sender, msg.value);
        }
    }

    /// @dev Chamado internamente para registrar recebimento de taxa em ERC-20.
    function notifyFee(address from, uint256 amount) external {
        // hook opcional: o EscrowVault já fez transfer antes. Mantém p/ auditoria/Indexer.
        emit FundsReceived(from, amount);
    }

    /// @notice Saque de RVM/ETH pelo admin (FUNDS_MANAGER_ROLE) para custeio de infra.
    /// Token = address(0) significa ETH nativo.
    function withdraw(address token, address to, uint256 amount)
        external
        onlyRole(FUNDS_MANAGER_ROLE)
    {
        if (to == address(0)) revert Treasury__ZeroAddress();
        if (amount == 0) revert Treasury__ZeroAmount();
        if (token == address(0)) {
            uint256 bal = address(this).balance;
            if (bal < amount) revert Treasury__InsufficientBalance();
            (bool ok, ) = to.call{value: amount}("");
            if (!ok) revert Treasury__TransferFailed();
        } else {
            uint256 bal = IERC20(token).balanceOf(address(this));
            if (bal < amount) revert Treasury__InsufficientBalance();
            bool ok = IERC20(token).transfer(to, amount);
            if (!ok) revert Treasury__TransferFailed();
        }
        emit FundsWithdrawn(to, token, amount);
    }

    receive() external payable {
        emit FundsReceived(msg.sender, msg.value);
    }

    error Treasury__ZeroAddress();
    error Treasury__ZeroAmount();
    error Treasury__InsufficientBalance();
    error Treasury__TransferFailed();
}
