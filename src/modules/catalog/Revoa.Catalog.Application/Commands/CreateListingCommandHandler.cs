using System.Numerics;
using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Commands;

public class CreateListingCommandHandler : IRequestHandler<CreateListingCommand, Result<string>>
{
    private readonly IListingRepository _listings;
    private readonly IProductNftService _productNft;
    private readonly IViaCepService _viaCep;

    public CreateListingCommandHandler(
        IListingRepository listings,
        IProductNftService productNft,
        IViaCepService viaCep)
    {
        _listings = listings;
        _productNft = productNft;
        _viaCep = viaCep;
    }

    public async Task<Result<string>> Handle(CreateListingCommand request, CancellationToken ct)
    {
        // Enrijece localização: se CEP presente e bairro/cidade ausentes, consulta ViaCEP.
        var bairro = request.Bairro;
        var cidade = request.Cidade;
        if (!string.IsNullOrWhiteSpace(request.Cep)
            && (string.IsNullOrWhiteSpace(bairro) || string.IsNullOrWhiteSpace(cidade)))
        {
            var addr = await _viaCep.GetByCepAsync(request.Cep!, ct);
            if (addr is not null)
            {
                bairro ??= addr.Bairro;
                cidade ??= addr.Cidade;
            }
        }

        var location = Location.Create(request.Lat, request.Lng, bairro, cidade, request.Cep);

        ProductDetails? productDetails = null;
        ServiceDetails? serviceDetails = null;
        if (request.Kind == ListingKind.Product)
        {
            productDetails = ProductDetails.Create(request.Condition ?? ProductCondition.Usado, request.Stock ?? 1);
        }
        else
        {
            serviceDetails = ServiceDetails.Create(
                request.UnitType ?? ServiceUnitType.PerService,
                request.Duration ?? 0,
                request.VoucherExpiryDays ?? 30);
        }

        Listing listing;
        try
        {
            listing = Listing.Create(
                request.Kind,
                request.Modo,
                request.Titulo,
                request.Descricao,
                request.Imagens,
                request.PrecoRvm,
                request.VendedorId,
                request.VendedorNome,
                request.VendedorAvatarUrl,
                location,
                request.CategoriaId,
                request.ComunidadeId,
                request.Visibilidade,
                productDetails,
                serviceDetails);
        }
        catch (DomainException ex)
        {
            return Result<string>.Fail(ex.Message);
        }

        // Persiste primeiro (idempotente do mint: tokenId atualiza via SetNftTokenId + Update).
        await _listings.AddAsync(listing, ct);

        // Mint-to-escrow SOMENTE para produto (trocar/repassar/doar). Serviço não tem NFT até a compra.
        if (listing.Kind == ListingKind.Product)
        {
            var tokenUri = $"https://assets.revoa.me/metadata/{listing.Id}";
            var onChainListingId = ToOnChainListingId(listing.Id);

            try
            {
                await _productNft.EnsureFaucetMinterRoleAsync(ct);
                var tokenId = await _productNft.MintToEscrowAsync(onChainListingId, tokenUri, ct);

                listing.SetNftTokenId((long)tokenId);
                await _listings.UpdateAsync(listing, ct);
            }
            catch (Exception)
            {
                // Mint falhou (chain indisponível?) — o anúncio fica sem NFT.
                // TODO: reconciliação assíncrona (mint retry via Indexer/outbox) quando chain voltar.
                throw;
            }
        }

        return Result<string>.Ok(listing.Id.ToString());
    }

    // O contrato espera um uint256 como listingId (referência numérica on-chain). Derivamos dos
    // primeiros 8 bytes do Guid (estável por anúncio). O tokenId autoritativo vem do retorno do mint.
    private static long ToOnChainListingId(Guid id)
    {
        var bytes = id.ToByteArray();
        var v = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(v);
    }
}
