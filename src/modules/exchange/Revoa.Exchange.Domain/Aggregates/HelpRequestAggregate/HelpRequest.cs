using Revoa.Abstractions;

namespace Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;

// Fila de doação/voluntariado (OOUX 13). Autor pede ajuda; doador cura selecionando o receptor.
public enum HelpRequestState
{
    Open,
    Selected,
    Withdrawn,
    Closed
}

// Aggregate "Pedido de Ajuda" (OOUX 13) — fila de receptor de anúncios doar/voluntariar.
// Autor é embed (Nome/Avatar) anti-N+1. SelectedTradeId liga à Trade criada na curadoria.
public class HelpRequest : AggregateRoot
{
    public Guid ListingId { get; private set; }

    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? AuthorAvatarUrl { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public HelpRequestState State { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Guid? SelectedTradeId { get; private set; }

    private HelpRequest() { }

    public static HelpRequest Create(
        Guid listingId,
        Guid authorId,
        string authorNome,
        string? authorAvatarUrl,
        string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            throw new DomainException("Mensagem do pedido de ajuda é obrigatória.");
        }

        if (mensagem.Length > 500)
        {
            throw new DomainException("Mensagem deve ter no máximo 500 caracteres.");
        }

        if (listingId == Guid.Empty)
        {
            throw new DomainException("ListingId é obrigatório.");
        }

        if (authorId == Guid.Empty)
        {
            throw new DomainException("Autor é obrigatório.");
        }

        return new HelpRequest
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            AuthorId = authorId,
            AuthorName = string.IsNullOrWhiteSpace(authorNome) ? "Usuário" : authorNome,
            AuthorAvatarUrl = authorAvatarUrl,
            Message = mensagem,
            State = HelpRequestState.Open,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Doador seleciona o receptor; SelectedTradeId liga à Trade criada (total 0).
    public void Select(Guid tradeId)
    {
        if (State != HelpRequestState.Open)
        {
            throw new DomainException("Apenas pedido aberto pode ser selecionado.");
        }

        if (tradeId == Guid.Empty)
        {
            throw new DomainException("TradeId é obrigatório.");
        }

        SelectedTradeId = tradeId;
        State = HelpRequestState.Selected;
    }

    public void Withdraw()
    {
        if (State != HelpRequestState.Open)
        {
            throw new DomainException("Apenas pedido aberto pode ser retirado.");
        }

        State = HelpRequestState.Withdrawn;
    }

    public void Close()
    {
        if (State != HelpRequestState.Selected)
        {
            throw new DomainException("Apenas pedido selecionado pode ser fechado.");
        }

        State = HelpRequestState.Closed;
    }
}
