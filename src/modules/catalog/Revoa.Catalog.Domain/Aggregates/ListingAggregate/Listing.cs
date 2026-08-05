using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

public enum ListingKind
{
    Product,
    Service
}

public enum ListingModo
{
    Trocar,
    Repassar,
    Doar,
    Voluntariar
}

public enum ListingVisibilidade
{
    Comunidade,
    Global,
    Ambos
}

public enum ListingStatus
{
    Rascunho,
    Ativo,
    EmAndamento,
    Concluido,
    Cancelado
}

// Aggregate "AnÃºncio" (OOUX objeto 7). kind (product|service) + modo + visibilidade.
// Vendedor Ã© embed (Nome/AvatarUrl) para evitar N+1 no feed. NftTokenId Ã© preenchido ao mintar
// produto (mint-to-escrow, UF-07..09). ServiÃ§o NÃƒO tem NFT atÃ© a compra (UF-13 mint-on-purchase).
public class Listing : AggregateRoot
{
    public ListingKind Kind { get; private set; }
    public ListingModo Modo { get; private set; }

    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public List<string> Imagens { get; private set; } = new();

    // long RVM (0 para doar/voluntariar).
    public long PrecoRvm { get; private set; }

    // Embed anti-N+1.
    public Guid VendedorId { get; private set; }
    public string VendedorNome { get; private set; } = string.Empty;
    public string? VendedorAvatarUrl { get; private set; }

    public Location Localizacao { get; private set; } = Location.Create(null, null, null, null, null);

    public Guid CategoriaId { get; private set; }

    // YAGNI: ComunidadeId nullable (mÃ³dulo Community ainda nÃ£o existe).
    public Guid? ComunidadeId { get; private set; }

    public ListingVisibilidade Visibilidade { get; private set; }

    // Preenchido ao mintar produto (mint-to-escrow). long dev (sequential tokenIds).
    public long? NftTokenId { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // VOs tipados por kind (sÃ³ um Ã© nÃ£o-null conforme o Kind).
    public ProductDetails? ProductDetails { get; private set; }
    public ServiceDetails? ServiceDetails { get; private set; }

    private Listing() { }

    public static Listing Create(
        ListingKind kind,
        ListingModo modo,
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
        ListingVisibilidade visibilidade,
        ProductDetails? productDetails = null,
        ServiceDetails? serviceDetails = null)
    {
        ValidateInvariants(kind, modo, precoRvm, visibilidade, comunidadeId, productDetails, serviceDetails);

        return new Listing
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Modo = modo,
            Titulo = titulo,
            Descricao = descricao,
            Imagens = imagens ?? new List<string>(),
            PrecoRvm = precoRvm,
            VendedorId = vendedorId,
            VendedorNome = string.IsNullOrWhiteSpace(vendedorNome) ? "UsuÃ¡rio" : vendedorNome,
            VendedorAvatarUrl = vendedorAvatarUrl,
            Localizacao = localizacao ?? Location.Create(null, null, null, null, null),
            CategoriaId = categoriaId,
            ComunidadeId = comunidadeId,
            Visibilidade = visibilidade,
            Status = ListingStatus.Ativo,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Preenche o NftTokenId apÃ³s mint-to-escrow (somente produtos).
    public void SetNftTokenId(long tokenId)
    {
        if (Kind != ListingKind.Product)
        {
            throw new DomainException("Apenas anÃºncios do tipo produto recebem NFT.");
        }

        NftTokenId = tokenId;
    }

    public void MarcarConcluido()
    {
        if (Status is ListingStatus.Concluido or ListingStatus.Cancelado)
        {
            throw new DomainException("AnÃºncio jÃ¡ estÃ¡ concluÃ­do ou cancelado.");
        }

        Status = ListingStatus.Concluido;
    }

    public void MarcarCancelado()
    {
        if (Status is ListingStatus.Concluido or ListingStatus.Cancelado)
        {
            throw new DomainException("AnÃºncio jÃ¡ estÃ¡ concluÃ­do ou cancelado.");
        }

        Status = ListingStatus.Cancelado;
    }

    private static void ValidateInvariants(
        ListingKind kind,
        ListingModo modo,
        long precoRvm,
        ListingVisibilidade visibilidade,
        Guid? comunidadeId,
        ProductDetails? productDetails,
        ServiceDetails? serviceDetails)
    {
        // Modo Ã— Kind (BUSINESS_RULES Â§1.2):
        //   Trocar     â†’ product | service
        //   Repassar   â†’ product
        //   Doar       â†’ product
        //   Voluntariarâ†’ service
        if (modo == ListingModo.Repassar && kind != ListingKind.Product)
        {
            throw new DomainException("Repassar Ã© exclusivo de produtos.");
        }

        if (modo == ListingModo.Doar && kind != ListingKind.Product)
        {
            throw new DomainException("Doar Ã© exclusivo de produtos.");
        }

        if (modo == ListingModo.Voluntariar && kind != ListingKind.Service)
        {
            throw new DomainException("Voluntariar Ã© exclusivo de serviÃ§os.");
        }

        // PreÃ§o: doar/voluntariar = 0 RVM; demais â‰¥ 0.
        if (precoRvm < 0)
        {
            throw new DomainException("PreÃ§o RVM nÃ£o pode ser negativo.");
        }

        if ((modo == ListingModo.Doar || modo == ListingModo.Voluntariar) && precoRvm != 0)
        {
            throw new DomainException("Doar/voluntariar deve ter preÃ§o 0 RVM.");
        }

        // VO por kind.
        if (kind == ListingKind.Product && productDetails is null)
        {
            throw new DomainException("Detalhes do produto sÃ£o obrigatÃ³rios para kind=Product.");
        }

        if (kind == ListingKind.Service && serviceDetails is null)
        {
            throw new DomainException("Detalhes do serviÃ§o sÃ£o obrigatÃ³rios para kind=Service.");
        }

        if (kind != ListingKind.Product && productDetails is not null)
        {
            throw new DomainException("Detalhes de produto nÃ£o aplicam a kind=Service.");
        }

        if (kind != ListingKind.Service && serviceDetails is not null)
        {
            throw new DomainException("Detalhes de serviÃ§o nÃ£o aplicam a kind=Product.");
        }

        // Visibilidade Comunidade exige ComunidadeId.
        if (visibilidade == ListingVisibilidade.Comunidade && comunidadeId is null)
        {
            throw new DomainException("Visibilidade Comunidade exige ComunidadeId.");
        }
    }
}
