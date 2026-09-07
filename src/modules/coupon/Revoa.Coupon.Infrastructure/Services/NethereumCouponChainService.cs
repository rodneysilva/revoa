using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json.Linq;
using Revoa.Coupon.Application.Services;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Coupon.Infrastructure.Services;

// Implementação Nethereum do ICouponChainService. Orquestra CouponRedeemer: criar/revogar assinados
// pela faucet (COUPON_ADMIN_ROLE); resgatar assinado pela carteira do usuário (msg.sender = usuário,
// que recebe o mint de RVM). Padrão uniforme: Account+Web3 por chamada, EstimateGasAsync antes de
// enviar (diagnóstico de revert), receipt.Status==0 → throw. Seletores de erro são parseados da
// exceção de estimate e mapeados em CouponChainException (o command converte p/ msg amigável).
public class NethereumCouponChainService : ICouponChainService
{
    // Evento CouponCreated(bytes32 indexed codeHash, uint256 amount, uint32 maxUses, uint64 expiry).
    private static readonly string CouponCreatedTopic0 =
        "0x" + BitConverter.ToString(
                Sha3Keccack.Current.CalculateHash(
                    Encoding.UTF8.GetBytes("CouponCreated(bytes32,uint256,uint32,uint64)")))
            .Replace("-", "")
            .ToLower();

    // COUPON_ADMIN_ROLE = keccak256("COUPON_ADMIN_ROLE").
    private static readonly byte[] CouponAdminRole =
        Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("COUPON_ADMIN_ROLE"));

    // Seletores (4 bytes) dos custom errors do CouponRedeemer → kind. Pré-computados em runtime.
    private static readonly Dictionary<string, CouponChainError> ErrorSelectors = BuildErrorSelectors();

    private static readonly string Abi = LoadEmbeddedAbi();

    private readonly CouponChainOptions _options;
    private readonly ILogger<NethereumCouponChainService> _logger;

    public NethereumCouponChainService(
        IOptions<CouponChainOptions> options,
        ILogger<NethereumCouponChainService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureFaucetCouponAdminRoleAsync(CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var contract = web3.Eth.GetContract(Abi, _options.Contracts.CouponRedeemer);

        var hasRole = contract.GetFunction("hasRole");
        var already = await hasRole.CallAsync<bool>(CouponAdminRole, account.Address);
        if (already)
        {
            return;
        }

        var grantRole = contract.GetFunction("grantRole");
        var gas = await grantRole.EstimateGasAsync(
            account.Address, null, null, CouponAdminRole, account.Address);
        await grantRole.SendTransactionAndWaitForReceiptAsync(
            account.Address, gas, null, ct, CouponAdminRole, account.Address);

        _logger.LogInformation("COUPON_ADMIN_ROLE concedida à faucet {Address} no CouponRedeemer.", account.Address);
    }

    public async Task<(string codeHash, string txHash)> CreateCouponAsync(
        string code, BigInteger amount, int maxUses, long expiryUnix, CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var contract = web3.Eth.GetContract(Abi, _options.Contracts.CouponRedeemer);

        var fn = contract.GetFunction("createCoupon");
        _logger.LogInformation(
            "createCoupon: admin={Admin} code=*** amount={Amount} maxUses={MaxUses} expiry={Expiry}",
            account.Address, amount.ToString(), maxUses, expiryUnix);

        var receipt = await SendAndClassifyAsync(fn, account.Address, ct,
            code, amount, (BigInteger)maxUses, (BigInteger)expiryUnix);

        var codeHash = ExtractCodeHashFromCouponCreated(receipt);

        _logger.LogInformation("createCoupon OK tx={Tx} codeHash={CodeHash}", receipt.TransactionHash, codeHash);
        return (codeHash, receipt.TransactionHash);
    }

    public async Task<string> RevokeCouponAsync(string code, CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var contract = web3.Eth.GetContract(Abi, _options.Contracts.CouponRedeemer);

        var fn = contract.GetFunction("revokeCoupon");
        _logger.LogInformation("revokeCoupon: admin={Admin} code=***", account.Address);

        var receipt = await SendAndClassifyAsync(fn, account.Address, ct, code);

        _logger.LogInformation("revokeCoupon OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> RedeemAsync(UserWallet userWallet, string code, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(userWallet.PrivateKey);
        await EnsureGasAsync(account.Address, ct);

        var contract = web3.Eth.GetContract(Abi, _options.Contracts.CouponRedeemer);

        var fn = contract.GetFunction("redeem");
        _logger.LogInformation("redeem: account={Account} code=***", account.Address);

        var receipt = await SendAndClassifyAsync(fn, account.Address, ct, code);

        _logger.LogInformation("redeem OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    // DEV: carteiras sem ETH falham no estimate com "insufficient funds" (Classify → Unknown
    // → "Falha ao resgatar cupom." sem pista). Repõe gás da faucet quando habilitado; no-op
    // em produção (Paymaster/AA). Espelha NethereumRvmService.FundGasIfEnabledAsync.
    private async Task EnsureGasAsync(string address, CancellationToken ct)
    {
        if (!_options.FundWalletGasOnCreate)
        {
            return;
        }

        var (_, faucetWeb3) = BuildFaucetWeb3();
        var balance = await faucetWeb3.Eth.GetBalance.SendRequestAsync(address);
        var minWei = UnitConversion.Convert.ToWei(0.01m);
        if (balance.Value >= minWei)
        {
            return;
        }

        var transfer = faucetWeb3.Eth.GetEtherTransferService();
        var txHash = await transfer.TransferEtherAsync(address, _options.FundWalletGasEther);
        _logger.LogInformation(
            "Faucet gas (redeem): {Ether} ETH para {Address} tx={Tx}",
            _options.FundWalletGasEther, address, txHash);
    }

    private (Account account, Web3 web3) BuildWeb3(string privateKey)
    {
        var account = new Account(privateKey, _options.ChainId);
        var web3 = new Web3(account, _options.RpcUrl);
        return (account, web3);
    }

    private (Account account, Web3 web3) BuildFaucetWeb3() => BuildWeb3(_options.FaucetPrivateKey);

    // Padrão uniforme: estimate → send → checa status 0. Estimate lança se a tx for reverter; o erro é
    // classificado pelo seletor (CouponChainException) p/ o command mapear msg amigável.
    private async Task<TransactionReceipt> SendAndClassifyAsync(
        Function fn, string from, CancellationToken ct, params object[] args)    {
        try
        {
            var gas = await fn.EstimateGasAsync(from, null, null, args);
            var receipt = await fn.SendTransactionAndWaitForReceiptAsync(from, gas, null, ct, args);

            if (receipt.Status.Value == 0)
            {
                throw new InvalidOperationException($"transação revertida tx={receipt.TransactionHash}");
            }

            return receipt;
        }
        catch (CouponChainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Registra a falha crua (estimate/send) — o command devolve só a msg amigável,
            // sem isso o "Falha ao resgatar cupom." não deixa pista nos logs.
            _logger.LogWarning(ex, "tx on-chain falhou ({From})", from);
            throw Classify(ex);
        }
    }

    // CouponCreated: codeHash é o 1º indexed topic (topics[1]), hex 32 bytes. Normaliza p/ lowercase sem 0x.
    private static string ExtractCodeHashFromCouponCreated(TransactionReceipt receipt)
    {
        foreach (var log in receipt.Logs)
        {
            var topics = log["topics"] as JArray;
            if (topics is null || topics.Count < 2)
            {
                continue;
            }

            if (!string.Equals(topics[0]?.ToString(), CouponCreatedTopic0, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return NormalizeHash(topics[1]!.ToString());
        }

        throw new InvalidOperationException("Evento CouponCreated não encontrado no receipt (codeHash indisponível).");
    }

    // Classifica uma falha on-chain pelo seletor do custom error presente na mensagem/data da exceção.
    // Varre o texto completo (message + Data + inner + ToString) buscando qualquer seletor conhecido (8 hex).
    // anvil/hardhat costumam trazer o seletor (4 bytes) na mensagem ou em Exception.Data.
    private static CouponChainException Classify(Exception ex)
    {
        var text = new StringBuilder();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            text.Append(current.Message).Append(' ').Append(current).Append(' ');
            if (current.Data is { Count: > 0 } data)
            {
                foreach (DictionaryEntry entry in data)
                {
                    if (entry.Value is not null)
                    {
                        text.Append(entry.Value).Append(' ');
                    }
                }
            }
        }

        var haystack = text.ToString();

        foreach (var (selector, error) in ErrorSelectors)
        {
            if (haystack.Contains(selector, StringComparison.OrdinalIgnoreCase))
            {
                return new CouponChainException(error, $"erro on-chain ({error})", ex);
            }
        }

        return new CouponChainException(CouponChainError.Unknown, "falha on-chain: " + ex.Message, ex);
    }

    private static string NormalizeHash(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }
        return hex.ToLowerInvariant();
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder("0x", bytes.Length * 2 + 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static Dictionary<string, CouponChainError> BuildErrorSelectors()
    {
        var signatures = new (string Signature, CouponChainError Error)[]
        {
            ("CouponRedeemer__AlreadyExists()", CouponChainError.AlreadyExists),
            ("CouponRedeemer__NotFound()", CouponChainError.NotFound),
            ("CouponRedeemer__Expired()", CouponChainError.Expired),
            ("CouponRedeemer__AlreadyUsed()", CouponChainError.AlreadyUsed),
            ("CouponRedeemer__MaxUsesReached()", CouponChainError.MaxUsesReached),
            ("CouponRedeemer__Revoked()", CouponChainError.Revoked),
            ("CouponRedeemer__ZeroAmount()", CouponChainError.ZeroAmount),
            ("CouponRedeemer__InvalidExpiry()", CouponChainError.InvalidExpiry)
        };

        var map = new Dictionary<string, CouponChainError>(StringComparer.OrdinalIgnoreCase);
        foreach (var (signature, error) in signatures)
        {
            var selectorBytes = Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(signature));
            var selectorHex = BitConverter.ToString(selectorBytes, 0, 4).Replace("-", "").ToLower();
            map[selectorHex] = error;
        }
        return map;
    }

    private static string LoadEmbeddedAbi()
    {
        var asm = typeof(NethereumCouponChainService).Assembly;
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("CouponRedeemer.abi.json"));
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("ABI do CouponRedeemer não encontrada como recurso embarcado.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
