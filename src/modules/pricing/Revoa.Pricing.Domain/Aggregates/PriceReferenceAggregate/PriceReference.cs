using Revoa.Abstractions;

namespace Revoa.Pricing.Domain.Aggregates.PriceReferenceAggregate;

// Referência de preço justo por categoria (UF-28, Fase 3). Um documento por CategoriaId (coleção
// PriceReferences, índice único por CategoriaId). Combina 4 fontes (resiliente — se uma falha,
// segue sem ela): (1) comunidade (mediana RVM dos anúncios ativos), (2) BRL admin-seed
// (rate global + override por slug), (3) IPCA IBGE (último mês %), (4) Ollama (refino opcional
// da sugestão justa — fallback mediana). Imutável após create — o refresh sempre recria; o repo
// faz Upsert por CategoriaId. Bump de Version é responsabilidade do repositório, nunca do aggregate.
public class PriceReference : AggregateRoot
{
    public Guid CategoriaId { get; private set; }
    public string? CategoriaSlug { get; private set; }

    // Base estatística comunitária.
    public long RvmMedian { get; private set; }
    public int SampleCount { get; private set; }

    // Semente BRL (admin): rate global efetiva + referência absoluta por categoria (se houver seed).
    public decimal BrlRate { get; private set; }
    public long? BrlReference { get; private set; }

    // Sugestão de preço justo em RVM (default = mediana; Ollama pode refinar).
    public long FairSuggestionRvm { get; private set; }

    // IPCA último mês coletado (null se indisponível nesta rodada).
    public decimal? LastIpcRate { get; private set; }
    public string? LastIpcMonth { get; private set; }

    // Flags das fontes que deram certo nesta rodada (ex.: "community,ipca,ollama").
    public string SourcesUsed { get; private set; } = string.Empty;

    public DateTime UpdatedAt { get; private set; }

    private PriceReference() { }

    // Factory: valida categoriaId≠empty. NÃO IncrementVersion (o repo faz Upsert).
    public static PriceReference Create(
        Guid categoriaId,
        string? categoriaSlug,
        long rvmMedian,
        int sampleCount,
        decimal brlRate,
        long? brlReference,
        long fairSuggestionRvm,
        decimal? lastIpcRate,
        string? lastIpcMonth,
        string sourcesUsed)
    {
        if (categoriaId == Guid.Empty)
        {
            throw new DomainException("Categoria é obrigatória.");
        }

        if (sampleCount < 0)
        {
            throw new DomainException("SampleCount não pode ser negativo.");
        }

        return new PriceReference
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaId,
            CategoriaSlug = categoriaSlug,
            RvmMedian = rvmMedian,
            SampleCount = sampleCount,
            BrlRate = brlRate,
            BrlReference = brlReference,
            FairSuggestionRvm = fairSuggestionRvm,
            LastIpcRate = lastIpcRate,
            LastIpcMonth = lastIpcMonth,
            SourcesUsed = sourcesUsed,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };
    }
}
