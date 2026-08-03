// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

/// @notice STUB LEVE — EntryPoint ERC-4337 canônico.
/// @dev O revoa NÃO reimplementa o EntryPoint. Em produção, plugar o contrato canônico auditado
/// (eth-infinitism/account-abstraction) no mesmo endereço. Esta interface existe apenas para o revoa
/// compilar/testar isoladamente. Substituir pela interface completa ao integrar. Ver ADR-0002.
interface IEntryPoint {
    struct UserOp {
        address sender;
        uint256 nonce;
        bytes initCode;
        bytes callData;
        uint256 callGasLimit;
        uint256 verificationGasLimit;
        uint256 preVerificationGas;
        uint256 maxFeePerGas;
        uint256 maxPriorityFeePerGas;
        bytes paymasterAndData;
        bytes signature;
    }

    /// @dev Canônico: processa um batch de UserOps. Mockado p/ testes do revoa.
    function handleOps(UserOp[] calldata ops, address beneficiary) external;
}
