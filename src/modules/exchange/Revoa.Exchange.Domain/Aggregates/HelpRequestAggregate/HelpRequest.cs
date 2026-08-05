using Revoa.Abstractions;

namespace Revoa.Exchange.Domain.Aggregates.HelpRequestAggregate;

// Fila de doaÃ§Ã£o/voluntariado (OOUX 13). Autor pede ajuda; doador cura selecionando o receptor.
public enum HelpRequestState
{
    Open,
    Selected,
    Withdrawn,
    Closed
}

// Aggregate "Pedido de Ajuda" (OOUX 13) â€” fila de receptor de anÃºncios doar/voluntariar.
// Autor Ã© embed (Nome/Avatar) anti-N+1. SelectedTradeId liga Ã  Trade criada na curadoria.
public class HelpRequest : AggregateRoot
{
    public Guid ListingId { get; private set; }

    public Guid AuthorId { get; private set; }
    public string AuthorNome { get; private set; } = string.Empty;
    public string? AuthorAvatarUrl { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

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
            throw new DomainException("Mensagem do pedido de ajuda Ã© obrigatÃ³ria.");
        }

        if (mensagem.Length > 500)
        {
            throw new DomainException("Mensagem deve ter no mÃ¡ximo 500 caracteres.");
        }

        if (listingId == Guid.Empty)
        {
            throw new DomainException("ListingId Ã© obrigatÃ³rio.");
        }

        if (authorId == Guid.Empty)
        {
            throw new DomainException("Autor Ã© obrigatÃ³rio.");
        }

        return new HelpRequest
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            AuthorId = authorId,
            AuthorNome = string.IsNullOrWhiteSpace(authorNome) ? "UsuÃ¡rio" : authorNome,
            AuthorAvatarUrl = authorAvatarUrl,
            Mensagem = mensagem,
            State = HelpRequestState.Open,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Doador seleciona o receptor; SelectedTradeId liga Ã  Trade criada (total 0).
    public void Select(Guid tradeId)
    {
        if (State != HelpRequestState.Open)
        {
            throw new DomainException("Apenas pedido aberto pode ser selecionado.");
        }

        if (tradeId == Guid.Empty)
        {
            throw new DomainException("TradeId Ã© obrigatÃ³rio.");
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
