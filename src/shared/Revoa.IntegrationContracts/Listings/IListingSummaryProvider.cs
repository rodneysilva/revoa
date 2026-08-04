namespace Revoa.IntegrationContracts.Listings;

// Porta (anti-corruption): permite que o módulo Exchange leia o contexto de um anúncio (kind, modo,
// preço, tokenId do NFT em escrow, vendedor) SEM acessar a coleção Listings (isolamento de módulos).
// O adapter vive em Revoa.Catalog.Infrastructure (lê o aggregate Listing).
// Tipos primitivos (string p/ enums) para não acoplar enums do Catalog neste contrato compartilhado.
public sealed record ListingSummary(
    Guid Id,
    string Kind,               // "Product" | "Service"
    string Modo,               // "Trocar" | "Repassar" | "Doar" | "Voluntariar"
    long PrecoRvm,             // 0 p/ doar/voluntariar
    Guid VendedorId,
    string VendedorNome,
    string? VendedorAvatarUrl,
    long? NftTokenId,          // produto: id do NFT (mint-to-escrow); serviço: null
    int VoucherExpiryDays,     // serviço: validade do voucher (default 30)
    string Status,             // "Ativo" | "EmAndamento" | "Concluido" | "Cancelado"
    bool IsDonation);          // true se Modo ∈ {Doar, Voluntariar}

public interface IListingSummaryProvider
{
    Task<ListingSummary?> GetByIdAsync(Guid listingId, CancellationToken ct = default);
}
