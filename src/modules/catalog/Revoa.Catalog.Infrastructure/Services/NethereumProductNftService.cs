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
using Revoa.Catalog.Application.Services;

namespace Revoa.Catalog.Infrastructure.Services;

// Implementação Nethereum do IProductNftService. Assina com a faucet (DEFAULT_ADMIN em dev).
// mintToEscrow(escrowVaultAddress, listingId, tokenUri) — o NFT nasce dentro do EscrowVault.
public class NethereumProductNftService : IProductNftService
{
    // ERC-721 Transfer(address indexed from, address indexed to, uint256 indexed tokenId).
    private const string TransferTopic0 = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

    private static readonly string Abi = LoadEmbeddedAbi();

    // MINTER_ROLE = keccak256("MINTER_ROLE") — byte[] (32) p/ o encoder de bytes32.
    private static readonly byte[] MinterRole =
        Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes("MINTER_ROLE"));

    private readonly CatalogChainOptions _options;
    private readonly ILogger<NethereumProductNftService> _logger;
    private readonly Account _account;
    private readonly Web3 _web3;
    private readonly Contract _nft;

    public NethereumProductNftService(
        IOptions<CatalogChainOptions> options,
        ILogger<NethereumProductNftService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _account = new Account(_options.FaucetPrivateKey, _options.ChainId);
        _web3 = new Web3(_account, _options.RpcUrl);
        _nft = _web3.Eth.GetContract(Abi, _options.Contracts.ProductNFT);
    }

    public async Task<BigInteger> MintToEscrowAsync(long listingId, string tokenUri, CancellationToken ct = default)
    {
        var fn = _nft.GetFunction("mintToEscrow");
        var escrow = _options.Contracts.EscrowVault;

        // EstimateGasAsync lança se a tx for reverter (diagnóstico de role/params).
        var gas = await fn.EstimateGasAsync(_account.Address, null, null, escrow, (BigInteger)listingId, tokenUri);
        _logger.LogInformation("ProductNFT mintToEscrow: escrow={Escrow} listing={Listing} gas={Gas}", escrow, listingId, gas);

        var receipt = await fn.SendTransactionAndWaitForReceiptAsync(
            _account.Address, gas, null, ct, escrow, (BigInteger)listingId, tokenUri);

        if (receipt.Status.Value == 0)
        {
            _logger.LogError("ProductNFT mintToEscrow REVERTIDO (status 0) tx={Tx}", receipt.TransactionHash);
            throw new InvalidOperationException($"mintToEscrow revertido tx={receipt.TransactionHash}");
        }

        var tokenId = ExtractTokenIdFromReceipt(receipt);
        _logger.LogInformation("ProductNFT mint OK tx={Tx} tokenId={TokenId} block={Block}",
            receipt.TransactionHash, tokenId, receipt.BlockNumber);

        return tokenId;
    }

    public async Task EnsureFaucetMinterRoleAsync(CancellationToken ct = default)
    {
        var hasRole = _nft.GetFunction("hasRole");
        var already = await hasRole.CallAsync<bool>(MinterRole, _account.Address);
        if (already)
        {
            return;
        }

        var grantRole = _nft.GetFunction("grantRole");
        var gas = await grantRole.EstimateGasAsync(_account.Address, null, null, MinterRole, _account.Address);
        await grantRole.SendTransactionAndWaitForReceiptAsync(
            _account.Address, gas, null, ct, MinterRole, _account.Address);

        _logger.LogInformation("MINTER_ROLE concedida à faucet {Address} no ProductNFT.", _account.Address);
    }

    // tokenId vem do evento Transfer (indexed) — leitura manual de topics, build-safe.
    private static BigInteger ExtractTokenIdFromReceipt(TransactionReceipt receipt)
    {
        foreach (var log in receipt.Logs)
        {
            var topics = log["topics"] as JArray;
            if (topics is null || topics.Count < 4)
            {
                continue;
            }

            if (!string.Equals(topics[0]?.ToString(), TransferTopic0, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return HexToBigInteger(topics[3]!.ToString());
        }

        return BigInteger.Zero;
    }

    private static BigInteger HexToBigInteger(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        return BigInteger.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static string LoadEmbeddedAbi()
    {
        var asm = typeof(NethereumProductNftService).Assembly;
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("ProductNFT.abi.json"));
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("ABI do ProductNFT não encontrada como recurso embarcado.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
