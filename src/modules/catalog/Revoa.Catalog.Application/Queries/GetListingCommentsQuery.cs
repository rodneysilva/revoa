using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Application.Queries;

// Comentários de um anúncio: raízes (parentId null) ou respostas diretas de parentId. Anônimo vê.
public sealed record GetListingCommentsQuery(Guid ListingId, Guid? ParentId)
    : IRequest<Result<IReadOnlyList<CommentDto>>>;

public class GetListingCommentsQueryHandler
    : IRequestHandler<GetListingCommentsQuery, Result<IReadOnlyList<CommentDto>>>
{
    private readonly ICommentRepository _comments;

    public GetListingCommentsQueryHandler(ICommentRepository comments)
    {
        _comments = comments;
    }

    public async Task<Result<IReadOnlyList<CommentDto>>> Handle(
        GetListingCommentsQuery request, CancellationToken ct)
    {
        var items = await _comments.GetByListingAsync(request.ListingId, request.ParentId, ct);
        var dtos = items.Select(CommentDtoMapper.From).ToList();
        if (dtos.Count == 0)
        {
            return Result<IReadOnlyList<CommentDto>>.Ok(dtos);
        }

        // Batch anti-N+1: o FE só oferece "Ver respostas" onde existem respostas.
        var counts = await _comments.GetChildrenCountsAsync(
            dtos.Select(d => d.Id).ToList(), ct);
        for (var i = 0; i < dtos.Count; i++)
        {
            dtos[i] = dtos[i] with { ChildrenCount = counts.GetValueOrDefault(dtos[i].Id) };
        }

        return Result<IReadOnlyList<CommentDto>>.Ok(dtos);
    }
}
