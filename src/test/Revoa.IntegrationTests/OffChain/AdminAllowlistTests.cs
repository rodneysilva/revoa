using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Revoa.Api;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Regressão do painel admin: Admin__Emails (env) chega como ESCALAR e o binder de
// Get<string[]> ignora — a policy "Admin" ficava vazia e negava tudo em containers,
// mesmo com o valor presente (funcionava só em dev, onde o appsettings traz array).
// Ver AdminAllowlist.
public class AdminAllowlistTests
{
    [Fact]
    public void Read_env_scalar_single_email()
    {
        var config = Build(("Admin:Emails", "rodneydocarmo@gmail.com"));

        AdminAllowlist.Read(config).Should().Equal("rodneydocarmo@gmail.com");
    }

    [Fact]
    public void Read_env_scalar_comma_separated_trims_and_ignores_empties()
    {
        var config = Build(("Admin:Emails", " a@b.com , c@d.com ; ; "));

        AdminAllowlist.Read(config).Should().Equal("a@b.com", "c@d.com");
    }

    [Fact]
    public void Read_indexed_array_still_binds()
    {
        var config = Build(("Admin:Emails:0", "x@y.z"), ("Admin:Emails:1", "w@v.u"));

        AdminAllowlist.Read(config).Should().Equal("x@y.z", "w@v.u");
    }

    [Fact]
    public void Read_empty_when_nothing_configured()
    {
        AdminAllowlist.Read(Build()).Should().BeEmpty();
    }

    private static IConfiguration Build(params (string Key, string? Value)[] pairs)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();
    }
}
