using System.Numerics;
using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.IntegrationContracts.Communities;

namespace Revoa.Catalog.Application.Commands;

public class CreateListingCommandHandler : IRequestHandler<CreateListingCommand, Result<string>>
{
    private readonly IListingRepository _listings;
    private readonly IProductNftService _productNft;
    private readonly IViaCepService _viaCep;
    private readonly IMembershipStatusChecker _membros;

    public CreateListingCommandHandler(
        IListingRepository listings,
        IProductNftService productNft,
        IViaCepService viaCep,
        IMembershipStatusChecker membros)
    {
        _listings = listings;
        _productNft = productNft;
        _viaCep = viaCep;
        _membros = membros;
    }

    public async Task<Result<string>> Handle(CreateListingCommand request, CancellationToken ct)
    {
        // Escopo a comunidade exige vínculo Active — a comunidade é círculo de
        // confiança; anúncio "da comunidade" de quem não participa não existe.
        if (request.CommunityId is not null)
        {
            var participante = await _membros.IsActiveMemberAsync(
                request.SellerId, request.CommunityId.Value, ct);
            if (!participante)
            {
                return Result<string>.Fail("Você não participa desta comunidade.");
            }
        }

        // Enrijece localização: se CEP presente e bairro/cidade ausentes, consulta ViaCEP.
        var bairro = request.Neighborhood;
        var cidade = request.City;
        if (!string.IsNullOrWhiteSpace(request.PostalCode)
            && (string.IsNullOrWhiteSpace(bairro) || string.IsNullOrWhiteSpace(cidade)))
        {
            var addr = await _viaCep.GetByCepAsync(request.PostalCode!, ct);
            if (addr is not null)
            {
                bairro ??= addr.Neighborhood;
                cidade ??= addr.City;
            }
        }

        var location = Location.Create(request.Lat, request.Lng, bairro, cidade, request.PostalCode);

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
                request.Mode,
                request.Title,
                request.Description,
                request.Imagens,
                request.PriceRvm,
                request.SellerId,
                request.SellerName,
                request.SellerAvatarUrl,
                location,
                request.CategoryId,
                request.CommunityId,
                request.Visibility,
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
            var onChainListingId = OnChainListingIds.FromGuid(listing.Id);

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

    // O id on-chain (uint256) do anúncio vem de OnChainListingIds (derivado estável do Guid);
    // o tokenId autoritativo é o retorno do mint.
}
