using System.Security.Claims;

namespace Revoa.Api.Controllers;

public sealed record RevoaUser(Guid UserId, string Nome, string? AvatarUrl);

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Lê o usuário do token JWT (claims sub/name/avatar — ownership NUNCA do body).
    /// Retorna null se o token não tiver um 'sub' Guid válido; o controller responde
    /// Unauthorized nesse caso. Substitui as cópias de ReadUser() nos controllers.
    /// </summary>
    public static RevoaUser? GetRevoaUser(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        {
            return null;
        }

        var nome = principal.FindFirst("name")?.Value
                   ?? principal.FindFirst("nickname")?.Value
                   ?? "Usuário";
        var avatar = principal.FindFirst("avatar")?.Value;

        return new RevoaUser(userId, nome, avatar);
    }
}
