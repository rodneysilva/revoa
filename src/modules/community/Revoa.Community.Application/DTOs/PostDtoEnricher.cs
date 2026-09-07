using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Repositories;

namespace Revoa.Community.Application.DTOs;

// Converte posts em PostDto com ChildrenCount/LikeCount/IsLiked/IsSaved em
// batch (anti-N+1) — compartilhado por GetCommunityPosts, GetSocialFeed,
// GetPublicPosts e GetSavedPosts. ViewerId null (anônimo) pula IsLiked/IsSaved;
// forceSavedIds pula a consulta quando a chamada já sabe que estão salvos
// (ex.: a própria listagem "Meus salvos").
public static class PostDtoEnricher
{
    public static async Task<IReadOnlyList<PostDto>> ToDtosAsync(
        IReadOnlyList<Post> posts,
        IPostRepository postsRepo,
        IPostLikeRepository likes,
        ISavedPostRepository savedRepo,
        Guid? viewerId,
        CancellationToken ct,
        IReadOnlyCollection<Guid>? forceSavedIds = null)
    {
        if (posts.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var ids = posts.Select(p => p.Id).ToList();
        var children = await postsRepo.GetChildrenCountsAsync(ids, ct);
        var likeCounts = await likes.GetCountsAsync(ids, ct);

        var likedIds = viewerId is null
            ? null
            : (await likes.GetLikedPostIdsAsync(viewerId.Value, ids, ct)).ToHashSet();

        var savedIds = forceSavedIds is not null
            ? forceSavedIds.ToHashSet()
            : viewerId is null
                ? null
                : (await savedRepo.GetSavedPostIdsAsync(viewerId.Value, ids, ct)).ToHashSet();

        return posts
            .Select(p => PostDtoMapper.From(
                p,
                children.GetValueOrDefault(p.Id),
                likeCounts.GetValueOrDefault(p.Id),
                likedIds?.Contains(p.Id) ?? false,
                savedIds?.Contains(p.Id) ?? false))
            .ToList();
    }
}
