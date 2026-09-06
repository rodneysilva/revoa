using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Api.Dev;

namespace Revoa.Api.Controllers;

// Endpoints DEV-ONLY (em produção retornam 404). Casca fina: a lógica de semeadura está no
// DevSeeder e os dados de demonstração no DevSeedData — aqui só o gate de ambiente + Policy=Admin.
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly DevSeeder _seeder;
    private readonly IWebHostEnvironment _env;

    public DevController(DevSeeder seeder, IWebHostEnvironment env)
    {
        _seeder = seeder;
        _env = env;
    }

    /// <summary>
    /// (Re)semeia o ambiente DEV completo: 10 usuários mock (+carteiras), catálogo vinculado aos
    /// mocks, 5 comunidades com membros e posts, e ~22 reviews (+reputação acumulada). Idempotente
    /// (limpa tudo por chaves determinísticas antes de re-inserir). Em produção retorna 404.
    /// Requer admin (não é mais anônimo — repovoar o banco é ação destrutiva).
    /// </summary>
    [HttpPost("seed-catalog")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> SeedCatalog(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await _seeder.SeedAsync(ct));
    }
}
