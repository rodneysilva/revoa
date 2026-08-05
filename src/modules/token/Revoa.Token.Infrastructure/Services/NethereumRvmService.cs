using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Revoa.Token.Application.Services;

namespace Revoa.Token.Infrastructure.Services;

// Implementação Nethereum do IRvmService. Assina com a chave da faucet (DEFAULT_ADMIN em dev).
public class NethereumRvmService : IRvmService
{
    private static readonly string Abi = LoadEmbeddedAbi();

    private readonly ChainOptions _options;
    private readonly ILogger<NethereumRvmService> _logger;
    private readonly Account _account;
    private readonly Web3 _web3;
    private readonly Contract _rvm;

    // MINTER_ROLE = keccak256("MINTER_ROLE") — mantido como byte[] (32 bytes) para o encoder
    // de bytes32 do Nethereum (passar hex string faz o encoder tratá-la como UTF8 e falhar).
    private static readonly byte[] MinterRole =
        Sha3Keccack.Current.CalculateHash(System.Text.Encoding.UTF8.GetBytes("MINTER_ROLE"));

    public NethereumRvmService(IOptions<ChainOptions> options, ILogger<NethereumRvmService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _account = new Account(_options.FaucetPrivateKey, _options.ChainId);
        _web3 = new Web3(_account, _options.RpcUrl);
        _rvm = _web3.Eth.GetContract(Abi, _options.Contracts.RVM);
    }

    public async Task<BigInteger> BalanceOfAsync(string address, CancellationToken ct = default)
    {
        var fn = _rvm.GetFunction("balanceOf");
        return await fn.CallAsync<BigInteger>(address);
    }

    public async Task<string> MintAsync(string to, BigInteger amount, CancellationToken ct = default)
    {
        var fn = _rvm.GetFunction("mint");

        // Estimativa de gas espelha o 'cast send'; além disso, EstimateGasAsync LANÇA se a tx for
        // reverter — útil para diagnosticar (nonce/gas são gerenciados pelo TransactionManager).
        var gas = await fn.EstimateGasAsync(_account.Address, null, null, to, amount);
        _logger.LogInformation("Mint: to={To} amount={Amount} gas={Gas}", to, amount, gas);

        var receipt = await fn.SendTransactionAndWaitForReceiptAsync(
            _account.Address, gas, null, ct, to, amount);

        if (receipt.Status.Value == 0)
        {
            _logger.LogError("Mint REVERTIDO (status 0) tx={Tx}", receipt.TransactionHash);
        }
        else
        {
            _logger.LogInformation("Mint OK tx={Tx} block={Block}", receipt.TransactionHash, receipt.BlockNumber);
        }

        return receipt.TransactionHash;
    }

    public async Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default)
    {
        var hasRole = _rvm.GetFunction("hasRole");
        var already = await hasRole.CallAsync<bool>(MinterRole, _account.Address);
        if (already)
        {
            return;
        }

        var grantRole = _rvm.GetFunction("grantRole");
        var gas = await grantRole.EstimateGasAsync(_account.Address, null, null, MinterRole, _account.Address);
        await grantRole.SendTransactionAndWaitForReceiptAsync(
            _account.Address, gas, null, ct, MinterRole, _account.Address);

        _logger.LogInformation("MINTER_ROLE concedida à faucet {Address}", _account.Address);
    }

    public async Task<string> FundGasAsync(string toAddress, decimal etherAmount, CancellationToken ct = default)
    {
        // Envia ETH da faucet (account[0] em dev) para a nova carteira. Nethereum EtherTransferService.
        var transfer = _web3.Eth.GetEtherTransferService();
        var txHash = await transfer.TransferEtherAsync(toAddress, etherAmount);
        _logger.LogInformation("Faucet gas: enviados {Ether} ETH para {Address} tx={Tx}", etherAmount, toAddress, txHash);
        return txHash;
    }

    public Task FundGasIfEnabledAsync(string toAddress, CancellationToken ct = default)
    {
        if (!_options.FundWalletGasOnCreate)
        {
            return Task.CompletedTask;
        }

        return FundGasAsync(toAddress, _options.FundWalletGasEther, ct);
    }

    private static string LoadEmbeddedAbi()
    {
        var asm = typeof(NethereumRvmService).Assembly;
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("RVM.abi.json"));
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("ABI do RVM não encontrada como recurso embarcado.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
