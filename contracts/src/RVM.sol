// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {ERC20} from "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";

/// @title RVM — crédito de troca on-chain (ERC-20 + AccessControl).
/// @notice NÃO é ativo financeiro (utility token — Lei 14.478). Moeda social comunitária.
/// Roles: DEFAULT_ADMIN, MINTER_ROLE (mint), BURNER_ROLE (burn).
contract RVM is ERC20, AccessControl {
    bytes32 public constant MINTER_ROLE = keccak256("MINTER_ROLE");
    bytes32 public constant BURNER_ROLE = keccak256("BURNER_ROLE");

    event Minted(address indexed to, uint256 amount, address indexed caller);
    event Burned(address indexed from, uint256 amount, address indexed caller);

    /// @param admin Carteira/governança que receberá DEFAULT_ADMIN_ROLE (geralmente o Treasury ou multisig da plataforma).
    /// @param initialSupply Cunhado para o admin no deploy (faucet/tesouraria). 0 = nenhum.
    constructor(address admin, uint256 initialSupply) ERC20("Revoa Credit", "RVM") {
        if (admin == address(0)) revert RVM__ZeroAddress();
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(MINTER_ROLE, admin); // bootstrap: admin concede MINTER/BURNER aos serviços depois
        _grantRole(BURNER_ROLE, admin);
        if (initialSupply > 0) {
            _mint(admin, initialSupply);
            emit Minted(admin, initialSupply, msg.sender);
        }
    }

    /// @notice Cunha RVM para `to`. Restrito a MINTER_ROLE (EscrowVault na liberação NÃO minta;
    /// minters = CouponRedeemer, faucet admin, deploy de tesouraria).
    function mint(address to, uint256 amount) external onlyRole(MINTER_ROLE) {
        if (to == address(0)) revert RVM__ZeroAddress();
        _mint(to, amount);
        emit Minted(to, amount, msg.sender);
    }

    /// @notice Queima RVM de `from` (demurrage, resgate de voucher, cancelamento).
    /// Restrito a BURNER_ROLE (operadores autorizados da plataforma).
    function burn(address from, uint256 amount) external onlyRole(BURNER_ROLE) {
        if (from == address(0)) revert RVM__ZeroAddress();
        _burn(from, amount);
        emit Burned(from, amount, msg.sender);
    }

    // AccessControl + ERC20 ambos implementam supportsInterface; ERC20 não sobrescreve.
    function supportsInterface(bytes4 interfaceId)
        public
        view
        virtual
        override(AccessControl)
        returns (bool)
    {
        return super.supportsInterface(interfaceId);
    }

    error RVM__ZeroAddress();
}
