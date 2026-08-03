// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

/// @notice STUB LEVE — Safe (Account Abstraction) + módulo 4337.
/// @dev O revoa NÃO reimplementa a Safe. Em produção, cada usuário terá uma Safe real
/// (Safe + módulo 4337 + recovery admin+timelock + signer Coinbase webauthn-solidity P-256).
/// Esta interface existe apenas p/ compilação/testes isolados. Substituir pelo SDK/Safe real. Ver ADR-0002.
interface ISafe {
    /// @dev Executa uma chamada em nome da carteira do usuário (após verificação do módulo 4337).
    function execTransactionFromModule(bytes calldata data) external returns (bool success);

    /// @dev Indica se a Safe é dona de um ativo / autoriza um operador (paymaster, módulo).
    function isOwner(address owner) external view returns (bool);
}

/// @notice Mock leve de uma Safe para testes do revoa (simula a custódia do usuário).
/// @dev NÃO use em produção — plugar a Safe real. Apenas para fluxos on-chain do revoa compilarem.
contract SafeMock {
    address[] public owners;

    constructor(address[] memory initialOwners) {
        owners = initialOwners;
    }

    function isOwner(address owner) external view returns (bool) {
        for (uint256 i = 0; i < owners.length; i++) {
            if (owners[i] == owner) return true;
        }
        return false;
    }
}
