using Revoa.Abstractions;

namespace Revoa.Reputation.Domain.Aggregates.ReviewAggregate;

// Avaliação pós-troca (UF-23). Um documento por direção (reviewer→reviewee) por trade (coleção
// Reviews, índice único TradeId+ReviewerId). Imutável após criação — não há mutators.
// Bump de Version é responsabilidade do repositório (insert com Version=1), nunca do aggregate.
public class Review : AggregateRoot
{
    public Guid TradeId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public string ReviewerNome { get; private set; } = string.Empty;
    public Guid RevieweeId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Review() { }

    // Factory: valida rating 1–5, reviewer≠reviewee e ids não-empty. CreatedAt=UtcNow.
    public static Review Create(
        Guid tradeId,
        Guid reviewerId,
        string reviewerNome,
        Guid revieweeId,
        int rating,
        string? comment)
    {
        if (tradeId == Guid.Empty)
        {
            throw new DomainException("Troca é obrigatória.");
        }

        if (reviewerId == Guid.Empty || revieweeId == Guid.Empty)
        {
            throw new DomainException("Avaliador e avaliado são obrigatórios.");
        }

        if (reviewerId == revieweeId)
        {
            throw new DomainException("Não é possível avaliar a si mesmo.");
        }

        if (rating < 1 || rating > 5)
        {
            throw new DomainException("Avaliação deve estar entre 1 e 5.");
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            TradeId = tradeId,
            ReviewerId = reviewerId,
            ReviewerNome = string.IsNullOrWhiteSpace(reviewerNome) ? "Usuário" : reviewerNome,
            RevieweeId = revieweeId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }
}
