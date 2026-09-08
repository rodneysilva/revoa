using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Revoa.Catalog.Application.Services;
using Revoa.Catalog.Domain.Repositories;

namespace Revoa.Catalog.Infrastructure;

// Reconcilia o mint-to-escrow de produtos que ficaram sem NFT (TODO Fase 3 resolvido): a
// criação do anúncio persiste ANTES do mint — se a chain falha no meio, o anúncio sobrevive
// sem NftTokenId e ninguém completava o mint. Este BackgroundService varre esses órfãos em
// lotes pequenos (mais antigos primeiro) e completa mint + SetNftTokenId.
//
// Resiliência: falha por item é isolada (o item volta no próximo ciclo); chain fora do ar
// adia o ciclo inteiro com UM log — sem spam nem tempestade de retry.
public class MintReconcilerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private const int BatchSize = 5;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MintReconcilerService> _logger;

    public MintReconcilerService(IServiceScopeFactory scopes, ILogger<MintReconcilerService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (true)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    return;
                }

                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ciclo de reconciliação de mint falhou — tenta de novo no próximo.");
            }
        }
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var listings = scope.ServiceProvider.GetRequiredService<IListingRepository>();
        var nft = scope.ServiceProvider.GetRequiredService<IProductNftService>();

        var pending = await listings.GetUnmintedProductsAsync(BatchSize, ct);
        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            await nft.EnsureFaucetMinterRoleAsync(ct);
        }
        catch (Exception ex)
        {
            // Chain indisponível: todo o ciclo seria falha — adia com um único log.
            _logger.LogInformation(
                ex,
                "Chain indisponível — reconciliação de mint adiada ({Count} produto(s) pendente(s)).",
                pending.Count);
            return;
        }

        foreach (var listing in pending)
        {
            try
            {
                var tokenUri = $"https://assets.revoa.me/metadata/{listing.Id}";
                var tokenId = await nft.MintToEscrowAsync(
                    OnChainListingIds.FromGuid(listing.Id), tokenUri, CancellationToken.None);

                listing.SetNftTokenId((long)tokenId);
                await listings.UpdateAsync(listing, CancellationToken.None);

                _logger.LogInformation(
                    "Mint reconciliado: anúncio {ListingId} → tokenId {TokenId}.", listing.Id, (long)tokenId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Mint falhou p/ anúncio {ListingId} — permanece pendente p/ o próximo ciclo.", listing.Id);
            }
        }
    }
}
