using System.Globalization;
using System.Numerics;
using System.Reflection;
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
using Revoa.Exchange.Application.Services;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.IntegrationContracts.UserWallets;

namespace Revoa.Exchange.Infrastructure.Services;

// Implementação Nethereum do IExchangeEscrowService. Orquestra EscrowVault + ServiceVoucher +
// RVM(approve). Cada tx assina com a parte correta (seller/buyer/faucet), criando Account+Web3
// por chamada. EstimateGasAsync antes de enviar (diagnóstico de revert); receipt.Status==0 → throw.
// tradeId/voucherId são lidos dos eventos TradeCreated/VoucherMinted do receipt (indexed topics).
public class NethereumExchangeEscrowService : IExchangeEscrowService
{
    // Evento TradeCreated(uint256 indexed tradeId, address indexed seller, address indexed buyer, ...).
    private const string TradeCreatedTopic0 = "0xab1c90eaa1d4de63540b3dab1711ceef61bebaf287ce38349e3747367f05b735";

    // Evento VoucherMinted(address indexed to, uint256 indexed listingId, uint256 indexed tokenId, uint64 expiry).
    private static readonly string VoucherMintedTopic0 =
        "0x" + BitConverter.ToString(
                Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("VoucherMinted(address,uint256,uint256,uint64)")))
            .Replace("-", "")
            .ToLower();

    // MINTER_ROLE = keccak256("MINTER_ROLE").
    private static readonly byte[] MinterRole =
        Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("MINTER_ROLE"));

    // ABI ERC-20 mínima (somente approve) — inline, não embarcada.
    private const string Erc20ApproveAbi =
        """[{"type":"function","name":"approve","inputs":[{"name":"spender","type":"address"},{"name":"amount","type":"uint256"}],"outputs":[{"name":"","type":"bool"}],"stateMutability":"nonpayable"}]""";

    private static readonly string EscrowAbi = LoadEmbeddedAbi("EscrowVault.abi.json");
    private static readonly string VoucherAbi = LoadEmbeddedAbi("ServiceVoucher.abi.json");

    private readonly ExchangeChainOptions _options;
    private readonly ILogger<NethereumExchangeEscrowService> _logger;

    public NethereumExchangeEscrowService(
        IOptions<ExchangeChainOptions> options,
        ILogger<NethereumExchangeEscrowService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string EscrowVaultAddress => _options.Contracts.EscrowVault;
    public string ProductNftAddress => _options.Contracts.ProductNFT;
    public string ServiceVoucherAddress => _options.Contracts.ServiceVoucher;

    public async Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var voucher = web3.Eth.GetContract(VoucherAbi, _options.Contracts.ServiceVoucher);

        var hasRole = voucher.GetFunction("hasRole");
        var already = await hasRole.CallAsync<bool>(ToHex(MinterRole), account.Address);
        if (already)
        {
            return;
        }

        var grantRole = voucher.GetFunction("grantRole");
        var gas = await grantRole.EstimateGasAsync(account.Address, null, null, ToHex(MinterRole), account.Address);
        await grantRole.SendTransactionAndWaitForReceiptAsync(
            account.Address, gas, null, ct, ToHex(MinterRole), account.Address);

        _logger.LogInformation("MINTER_ROLE concedida à faucet {Address} no ServiceVoucher.", account.Address);
    }

    public async Task<(long onChainTradeId, string txHash)> CreateTradeAsync(
        UserWallet sellerWallet,
        string buyerAddr,
        long total,
        TradeKind kind,
        string assetContract,
        long tokenId,
        CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(sellerWallet.PrivateKey);
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var kindByte = kind == TradeKind.Product ? BigInteger.Zero : BigInteger.One;
        var fn = escrow.GetFunction("createTrade");

        _logger.LogInformation(
            "createTrade: seller={Seller} buyer={Buyer} total={Total} kind={Kind} asset={Asset} tokenId={TokenId}",
            account.Address, buyerAddr, total, kindByte, assetContract, tokenId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct,
            buyerAddr, (BigInteger)total, kindByte, assetContract, (BigInteger)tokenId);

        var onChainTradeId = ExtractTradeIdFromTradeCreated(receipt);

        _logger.LogInformation("createTrade OK tx={Tx} tradeId={TradeId}", receipt.TransactionHash, onChainTradeId);
        return (onChainTradeId, receipt.TransactionHash);
    }

    public async Task<string> ApproveRvmAsync(
        UserWallet ownerWallet,
        string spenderAddr,
        long amount,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return string.Empty;
        }

        var (account, web3) = BuildWeb3(ownerWallet.PrivateKey);
        var rvm = web3.Eth.GetContract(Erc20ApproveAbi, _options.Contracts.RVM);

        var fn = rvm.GetFunction("approve");
        _logger.LogInformation("RVM approve: owner={Owner} spender={Spender} amount={Amount}",
            account.Address, spenderAddr, amount);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, spenderAddr, (BigInteger)amount);
        _logger.LogInformation("RVM approve OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> FundTradeAsync(UserWallet buyerWallet, long onChainTradeId, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(buyerWallet.PrivateKey);
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var fn = escrow.GetFunction("fundTrade");
        _logger.LogInformation("fundTrade: buyer={Buyer} tradeId={TradeId}", account.Address, onChainTradeId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)onChainTradeId);
        _logger.LogInformation("fundTrade OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<(long voucherId, string txHash)> MintVoucherAsync(
        long listingId,
        string buyerAddr,
        int expiryDays,
        CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var voucher = web3.Eth.GetContract(VoucherAbi, _options.Contracts.ServiceVoucher);

        var expiryUnix = new DateTimeOffset(DateTime.UtcNow.AddDays(expiryDays > 0 ? expiryDays : 30))
            .ToUnixTimeSeconds();

        var fn = voucher.GetFunction("mintOnPurchase");
        _logger.LogInformation("mintOnPurchase: to={To} listingId={Listing} expiry={Expiry}",
            buyerAddr, listingId, expiryUnix);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct,
            buyerAddr, (BigInteger)listingId, (BigInteger)expiryUnix);

        var voucherId = ExtractVoucherIdFromVoucherMinted(receipt);

        _logger.LogInformation("mintOnPurchase OK tx={Tx} voucherId={VoucherId}", receipt.TransactionHash, voucherId);
        return (voucherId, receipt.TransactionHash);
    }

    public async Task<string> RedeemAsync(UserWallet buyerWallet, long voucherId, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(buyerWallet.PrivateKey);
        var voucher = web3.Eth.GetContract(VoucherAbi, _options.Contracts.ServiceVoucher);

        var fn = voucher.GetFunction("redeem");
        _logger.LogInformation("redeem: buyer={Buyer} voucherId={VoucherId}", account.Address, voucherId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)voucherId);
        _logger.LogInformation("redeem OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> ReleaseAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(partyWallet.PrivateKey);
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var fn = escrow.GetFunction("release");
        _logger.LogInformation("release: party={Party} tradeId={TradeId}", account.Address, onChainTradeId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)onChainTradeId);
        _logger.LogInformation("release OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> OpenDisputeAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(partyWallet.PrivateKey);
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var fn = escrow.GetFunction("openDispute");
        _logger.LogInformation("openDispute: party={Party} tradeId={TradeId}", account.Address, onChainTradeId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)onChainTradeId);
        _logger.LogInformation("openDispute OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> ClaimArbitratorAsync(long onChainTradeId, bool releaseToSeller, CancellationToken ct = default)
    {
        var (account, web3) = BuildFaucetWeb3();
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var fn = escrow.GetFunction("claimArbitrator");
        _logger.LogInformation("claimArbitrator: arbitrator={Arb} tradeId={TradeId} releaseToSeller={Release}",
            account.Address, onChainTradeId, releaseToSeller);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)onChainTradeId, releaseToSeller);
        _logger.LogInformation("claimArbitrator OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    public async Task<string> CancelAsync(UserWallet partyWallet, long onChainTradeId, CancellationToken ct = default)
    {
        var (account, web3) = BuildWeb3(partyWallet.PrivateKey);
        var escrow = web3.Eth.GetContract(EscrowAbi, _options.Contracts.EscrowVault);

        var fn = escrow.GetFunction("cancel");
        _logger.LogInformation("cancel: party={Party} tradeId={TradeId}", account.Address, onChainTradeId);

        var receipt = await SendAndGetReceiptAsync(fn, account.Address, ct, (BigInteger)onChainTradeId);
        _logger.LogInformation("cancel OK tx={Tx}", receipt.TransactionHash);
        return receipt.TransactionHash;
    }

    private (Account account, Web3 web3) BuildWeb3(string privateKey)
    {
        var account = new Account(privateKey, _options.ChainId);
        var web3 = new Web3(account, _options.RpcUrl);
        return (account, web3);
    }

    private (Account account, Web3 web3) BuildFaucetWeb3() => BuildWeb3(_options.FaucetPrivateKey);

    // Padrão uniforme: estimate → send → checa status 0. Estimate lança se a tx for reverter.
    private static async Task<TransactionReceipt> SendAndGetReceiptAsync(
        Function fn, string from, CancellationToken ct, params object[] args)
    {
        var gas = await fn.EstimateGasAsync(from, null, null, args);
        var receipt = await fn.SendTransactionAndWaitForReceiptAsync(from, gas, null, ct, args);

        if (receipt.Status.Value == 0)
        {
            throw new InvalidOperationException($"transação revertida tx={receipt.TransactionHash}");
        }

        return receipt;
    }

    // TradeCreated: tradeId é o 1º indexed topic (topics[1]).
    private static long ExtractTradeIdFromTradeCreated(TransactionReceipt receipt)
    {
        foreach (var log in receipt.Logs)
        {
            var topics = log["topics"] as JArray;
            if (topics is null || topics.Count < 2)
            {
                continue;
            }

            if (!string.Equals(topics[0]?.ToString(), TradeCreatedTopic0, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return (long)HexToBigInteger(topics[1]!.ToString());
        }

        throw new InvalidOperationException("Evento TradeCreated não encontrado no receipt (tradeId indisponível).");
    }

    // VoucherMinted: tokenId é o 3º indexed topic (topics[3]).
    private static long ExtractVoucherIdFromVoucherMinted(TransactionReceipt receipt)
    {
        foreach (var log in receipt.Logs)
        {
            var topics = log["topics"] as JArray;
            if (topics is null || topics.Count < 4)
            {
                continue;
            }

            if (!string.Equals(topics[0]?.ToString(), VoucherMintedTopic0, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return (long)HexToBigInteger(topics[3]!.ToString());
        }

        throw new InvalidOperationException("Evento VoucherMinted não encontrado no receipt (voucherId indisponível).");
    }

    private static BigInteger HexToBigInteger(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        return BigInteger.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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

    private static string LoadEmbeddedAbi(string fileName)
    {
        var asm = typeof(NethereumExchangeEscrowService).Assembly;
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith(fileName));
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"ABI {fileName} não encontrada como recurso embarcado.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
