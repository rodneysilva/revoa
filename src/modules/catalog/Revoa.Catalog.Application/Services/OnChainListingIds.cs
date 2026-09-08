namespace Revoa.Catalog.Application.Services;

// Id on-chain de um anúncio: o contrato ProductNFT espera um uint256 e o listing tem Guid.
// Derivação estável pelos primeiros 8 bytes do Guid — usada no mint-to-escrow da criação
// (CreateListingCommandHandler) e na reconciliação (MintReconcilerService), sempre igual.
public static class OnChainListingIds
{
    public static long FromGuid(Guid id)
    {
        var bytes = id.ToByteArray();
        var v = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(v);
    }
}
