using Microsoft.Extensions.Configuration;

namespace Revoa.Api;

// Allowlist de admins (Admin:Emails). Fontes: array em appsettings/harness OU
// escalar nos containers (env Admin__Emails — um e-mail ou lista por vírgula).
// O binder de Get<string[]> ignora escalar, o que deixava a policy "Admin" vazia
// (negando TUDO) em hosts configurados só por env, mesmo com o valor presente.
public static class AdminAllowlist
{
    public static string[] Read(IConfiguration configuration)
    {
        var emails = configuration.GetSection("Admin:Emails").Get<string[]>();
        if (emails is { Length: > 0 })
        {
            return emails;
        }

        var raw = configuration["Admin:Emails"];
        return string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
