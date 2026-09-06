using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

public enum ListingKind
{
    Product,
    Service
}

public enum ListingMode
{
    Trade,
    Resell,
    Donate,
    Volunteer
}

public enum ListingVisibility
{
    Community,
    Global,
    Both
}

public enum ListingStatus
{
    Draft,
    Active,
    InProgress,
    Completed,
    Cancelled
}

// Aggregate "Anúncio" (OOUX objeto 7). kind (product|service) + modo + visibilidade.
// Vendedor é embed (Nome/AvatarUrl) para evitar N+1 no feed. NftTokenId é preenchido ao mintar
// produto (mint-to-escrow, UF-07..09). Serviço NÃO tem NFT até a compra (UF-13 mint-on-purchase).
public class Listing : AggregateRoot
{
    public ListingKind Kind { get; private set; }
    public ListingMode Mode { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public List<string> Imagens { get; private set; } = new();

    // long RVM (0 para doar/voluntariar).
    public long PriceRvm { get; private set; }

    // Embed anti-N+1.
    public Guid SellerId { get; private set; }
    public string SellerName { get; private set; } = string.Empty;
    public string? SellerAvatarUrl { get; private set; }

    public Location Localizacao { get; private set; } = Location.Create(null, null, null, null, null);

    public Guid CategoryId { get; private set; }

    // YAGNI: CommunityId nullable (módulo Community ainda não existe).
    public Guid? CommunityId { get; private set; }

    public ListingVisibility Visibility { get; private set; }

    // Preenchido ao mintar produto (mint-to-escrow). long dev (sequential tokenIds).
    public long? NftTokenId { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // VOs tipados por kind (só um é não-null conforme o Kind).
    public ProductDetails? ProductDetails { get; private set; }
    public ServiceDetails? ServiceDetails { get; private set; }

    private Listing() { }

    public static Listing Create(
        ListingKind kind,
        ListingMode modo,
        string titulo,
        string descricao,
        List<string> imagens,
        long precoRvm,
        Guid vendedorId,
        string vendedorNome,
        string? vendedorAvatarUrl,
        Location localizacao,
        Guid categoriaId,
        Guid? comunidadeId,
        ListingVisibility visibilidade,
        ProductDetails? productDetails = null,
        ServiceDetails? serviceDetails = null)
    {
        ValidateInvariants(kind, modo, precoRvm, visibilidade, comunidadeId, productDetails, serviceDetails);

        return new Listing
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Mode = modo,
            Title = titulo,
            Description = descricao,
            Imagens = imagens ?? new List<string>(),
            PriceRvm = precoRvm,
            SellerId = vendedorId,
            SellerName = string.IsNullOrWhiteSpace(vendedorNome) ? "Usuário" : vendedorNome,
            SellerAvatarUrl = vendedorAvatarUrl,
            Localizacao = localizacao ?? Location.Create(null, null, null, null, null),
            CategoryId = categoriaId,
            CommunityId = comunidadeId,
            Visibility = visibilidade,
            Status = ListingStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Preenche o NftTokenId após mint-to-escrow (somente produtos).
    public void SetNftTokenId(long tokenId)
    {
        if (Kind != ListingKind.Product)
        {
            throw new DomainException("Apenas anúncios do tipo produto recebem NFT.");
        }

        NftTokenId = tokenId;
    }

    public void MarcarConcluido()
    {
        if (Status is ListingStatus.Completed or ListingStatus.Cancelled)
        {
            throw new DomainException("Anúncio já está concluído ou cancelado.");
        }

        Status = ListingStatus.Completed;
    }

    public void MarcarCancelado()
    {
        if (Status is ListingStatus.Completed or ListingStatus.Cancelled)
        {
            throw new DomainException("Anúncio já está concluído ou cancelado.");
        }

        Status = ListingStatus.Cancelled;
    }

    private static void ValidateInvariants(
        ListingKind kind,
        ListingMode modo,
        long precoRvm,
        ListingVisibility visibilidade,
        Guid? comunidadeId,
        ProductDetails? productDetails,
        ServiceDetails? serviceDetails)
    {
        // Modo × Kind (BUSINESS_RULES §1.2):
        //   Trocar     → product | service
        //   Repassar   → product
        //   Doar       → product
        //   Voluntariar→ service
        if (modo == ListingMode.Resell && kind != ListingKind.Product)
        {
            throw new DomainException("Repassar é exclusivo de produtos.");
        }

        if (modo == ListingMode.Donate && kind != ListingKind.Product)
        {
            throw new DomainException("Doar é exclusivo de produtos.");
        }

        if (modo == ListingMode.Volunteer && kind != ListingKind.Service)
        {
            throw new DomainException("Voluntariar é exclusivo de serviços.");
        }

        // Preço: doar/voluntariar = 0 RVM; demais ≥ 0.
        if (precoRvm < 0)
        {
            throw new DomainException("Preço RVM não pode ser negativo.");
        }

        if ((modo == ListingMode.Donate || modo == ListingMode.Volunteer) && precoRvm != 0)
        {
            throw new DomainException("Doar/voluntariar deve ter preço 0 RVM.");
        }

        // VO por kind.
        if (kind == ListingKind.Product && productDetails is null)
        {
            throw new DomainException("Detalhes do produto são obrigatórios para kind=Product.");
        }

        if (kind == ListingKind.Service && serviceDetails is null)
        {
            throw new DomainException("Detalhes do serviço são obrigatórios para kind=Service.");
        }

        if (kind != ListingKind.Product && productDetails is not null)
        {
            throw new DomainException("Detalhes de produto não aplicam a kind=Service.");
        }

        if (kind != ListingKind.Service && serviceDetails is not null)
        {
            throw new DomainException("Detalhes de serviço não aplicam a kind=Product.");
        }

        // Visibilidade Comunidade exige CommunityId.
        if (visibilidade == ListingVisibility.Community && comunidadeId is null)
        {
            throw new DomainException("Visibilidade Comunidade exige CommunityId.");
        }
    }
}
