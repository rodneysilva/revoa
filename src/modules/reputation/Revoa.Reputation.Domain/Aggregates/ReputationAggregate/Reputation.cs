using Revoa.Abstractions;

namespace Revoa.Reputation.Domain.Aggregates.ReputationAggregate;

// Score de reputação de um usuário (OOUX objeto "Reputação"). Um documento por usuário
// (coleção Reputations, índice único por UserId). Atualizado por eventos: doação/voluntariado
// (DonationCompletedEvent) e avaliações (ApplyReview). Bump de Version é responsabilidade do
// repositório (upsert por UserId) — NUNCA dos mutators deste aggregate.
public class Reputation : AggregateRoot
{
    public Guid UserId { get; private set; }
    public long Points { get; private set; }
    public long HelpPoints { get; private set; }
    public int DonationsCount { get; private set; }
    public int VolunteerCount { get; private set; }
    public int ReviewsCount { get; private set; }
    public long RatingsSum { get; private set; }

    private Reputation() { }

    public static Reputation Create(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        return new Reputation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = 0,
            HelpPoints = 0,
            DonationsCount = 0,
            VolunteerCount = 0,
            ReviewsCount = 0,
            RatingsSum = 0,
            Version = 1
        };
    }

    // Recompensa de doação/voluntariado. isVolunteer=true conta como voluntariado; doação caso contrário.
    public void ApplyDonationReward(long reputationPoints, long helpPoints, bool isVolunteer)
    {
        Points += reputationPoints;
        HelpPoints += helpPoints;

        if (isVolunteer)
        {
            VolunteerCount++;
        }
        else
        {
            DonationsCount++;
        }
    }

    // Avaliação pós-troca (1–5 estrelas). Lança DomainException se fora do intervalo.
    public void ApplyReview(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new DomainException("Avaliação deve estar entre 1 e 5.");
        }

        ReviewsCount++;
        RatingsSum += rating;
    }

    // Nível/faixa de reputação baseado em Points.
    public string Level => Points switch
    {
        < 50 => "Iniciante",
        < 200 => "Ajudante",
        < 500 => "Mentor",
        _ => "Guardião"
    };

    // Média de avaliações (0 se ainda não recebeu nenhuma).
    public double AvgRating => ReviewsCount > 0 ? (double)RatingsSum / ReviewsCount : 0;
}
