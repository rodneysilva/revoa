using Revoa.Abstractions;

namespace Revoa.Moderation.Domain.Aggregates.ReportAggregate;

// Alvo de uma denúncia (UF-24/25). Pode ser um anúncio, post, usuário ou comentário.
public enum ReportTarget
{
    Listing,
    Post,
    User,
    Comment
}

// Motivo da denúncia.
public enum ReportReason
{
    Spam,
    Inappropriate,
    Scam,
    Other
}

// Estado da denúncia: em aberto (aguardando moderação) ou resolvida.
public enum ReportStatus
{
    Open,
    Resolved
}

// Ação tomada pelo moderador ao resolver (UF-25). Banned dispara o banimento do usuário via evento.
public enum ResolutionAction
{
    Dismissed,
    Warned,
    Banned
}

// Denúncia (coleção Reports, isolada do módulo Identity). Banimento de usuário NÃO acontece aqui —
// o módulo publica UserBanRequestedEvent e o Identity consome (isolamento de bounded contexts).
// Bump de Version é responsabilidade do repositório (UpdateAsync), nunca do mutator Resolve().
public class Report : AggregateRoot
{
    public Guid ReporterId { get; private set; }
    public string ReporterNome { get; private set; } = string.Empty;
    public ReportTarget TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Details { get; private set; }
    public ReportStatus Status { get; private set; }
    public ResolutionAction? Action { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Report() { }

    // Factory: valida ids não-empty. Status=Open, Action=null. CreatedAt=UtcNow. Version=1.
    public static Report Create(
        Guid reporterId,
        string reporterNome,
        ReportTarget targetType,
        Guid targetId,
        ReportReason reason,
        string? details)
    {
        if (reporterId == Guid.Empty)
        {
            throw new DomainException("Denunciante é obrigatório.");
        }

        if (targetId == Guid.Empty)
        {
            throw new DomainException("Alvo da denúncia é obrigatório.");
        }

        if (!Enum.IsDefined(reason))
        {
            throw new DomainException("Motivo de denúncia inválido.");
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new DomainException("Tipo de alvo inválido.");
        }

        return new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            ReporterNome = string.IsNullOrWhiteSpace(reporterNome) ? "Usuário" : reporterNome,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            Status = ReportStatus.Open,
            Action = null,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Resolve a denúncia (Open→Resolved): registra ação/nota/responsável/data. NÃO IncrementVersion
    // — o bump fica no repositório (optimistic locking). Erro se já resolvida.
    public void Resolve(string resolvedBy, ResolutionAction action, string? note)
    {
        if (Status == ReportStatus.Resolved)
        {
            throw new DomainException("Esta denúncia já foi resolvida.");
        }

        if (!Enum.IsDefined(action))
        {
            throw new DomainException("Ação de resolução inválida.");
        }

        Status = ReportStatus.Resolved;
        Action = action;
        ResolvedBy = string.IsNullOrWhiteSpace(resolvedBy) ? "Admin" : resolvedBy;
        ResolutionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ResolvedAt = DateTime.UtcNow;
    }
}
