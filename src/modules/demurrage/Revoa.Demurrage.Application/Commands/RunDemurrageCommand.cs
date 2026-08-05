using System.Numerics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.Demurrage.Application.DTOs;
using Revoa.Demurrage.Application.Options;
using Revoa.Demurrage.Domain.Aggregates.DemurrageRunAggregate;
using Revoa.Demurrage.Domain.Repositories;
using Revoa.IntegrationContracts.Accounts;
using Revoa.IntegrationContracts.Admin;
using Revoa.Token.Application.Services;
using Revoa.Token.Domain;

namespace Revoa.Demurrage.Application.Commands;

// Execução real do demurrage (Admin): queima RateBps% do saldo de cada carteira acima do piso,
// on-chain (faucet BURNER_ROLE). Resiliente: falha em 1 carteira (ex.: saldo mudou entre leitura e
// queima) é logada e CONTINUA — não derruba o run inteiro. Persiste o resultado em DemurrageRuns.
//
// Os parâmetros (RateBps/Floor/Enabled) vêm do IParameterStore (runtime, UF-30) com fallback para
// os defaults do DemurrageOptions. Se Enabled=false (store) o run falha com mensagem clara — o
// admin precisa reativar o módulo antes de executar queimas reais.
//
// Importante: as txs on-chain (BurnAsync) usam CancellationToken.None p/ não serem canceladas por
// um timeout/disconexão do request HTTP (igual ao faucet). A gravação do histórico (AddAsync) idem.
public sealed record RunDemurrageCommand(string ExecutedBy) : IRequest<Result<DemurrageRunDto>>;

public class RunDemurrageCommandHandler
    : IRequestHandler<RunDemurrageCommand, Result<DemurrageRunDto>>
{
    private static readonly BigInteger Unit = BigInteger.Pow(10, RvmConstants.Decimals);

    private readonly IRvmService _rvm;
    private readonly IWalletAddressReader _wallets;
    private readonly IDemurrageRunRepository _repo;
    private readonly DemurrageOptions _options;
    private readonly IParameterStore _parameters;
    private readonly ILogger<RunDemurrageCommandHandler> _logger;

    public RunDemurrageCommandHandler(
        IRvmService rvm,
        IWalletAddressReader wallets,
        IDemurrageRunRepository repo,
        IOptions<DemurrageOptions> options,
        IParameterStore parameters,
        ILogger<RunDemurrageCommandHandler> logger)
    {
        _rvm = rvm;
        _wallets = wallets;
        _repo = repo;
        _options = options.Value;
        _parameters = parameters;
        _logger = logger;
    }

    public async Task<Result<DemurrageRunDto>> Handle(RunDemurrageCommand request, CancellationToken ct)
    {
        // Parâmetros runtime com fallback para os defaults do IOptions. GetAsync<T> (T sem
        // constraint) colapsa T? para o próprio tipo em value types -> valor sempre concreto.
        var monthlyRateBps = await _parameters.GetAsync("Demurrage.MonthlyRateBps", _options.MonthlyRateBps, ct);
        var floorRvm = await _parameters.GetAsync("Demurrage.FloorRvm", _options.FloorRvm, ct);
        var enabled = await _parameters.GetAsync("Demurrage.Enabled", _options.Enabled, ct);

        if (!enabled)
        {
            return Result<DemurrageRunDto>.Fail("Demurrage desativado.");
        }

        if (string.IsNullOrWhiteSpace(request.ExecutedBy))
        {
            return Result<DemurrageRunDto>.Fail("ExecutedBy é obrigatório.");
        }

        // Garante (idempotente) BURNER_ROLE na faucet antes de qualquer queima.
        await _rvm.EnsureFaucetBurnerRoleAsync(ct);

        var wallets = await _wallets.GetAllAsync(ct);

        BigInteger totalBurnRaw = BigInteger.Zero;
        var affected = 0;
        var skipped = 0;

        foreach (var w in wallets)
        {
            if (string.IsNullOrWhiteSpace(w.Address))
            {
                skipped++;
                continue;
            }

            BigInteger balance;
            try
            {
                balance = await _rvm.BalanceOfAsync(w.Address, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Run demurrage: falha ao ler saldo de {Address} — pulando.", w.Address);
                skipped++;
                continue;
            }

            if (balance / Unit <= floorRvm)
            {
                skipped++;
                continue;
            }

            var burnRaw = balance * monthlyRateBps / 10000;
            if (burnRaw <= 0)
            {
                continue;
            }

            try
            {
                // CT.None: a queima on-chain não pode ser cancelada pelo request HTTP.
                var txHash = await _rvm.BurnAsync(w.Address, burnRaw, CancellationToken.None);
                _logger.LogInformation(
                    "Demurrage: queimado {Raw} raw de {Address} tx={Tx}", burnRaw, w.Address, txHash);

                totalBurnRaw += burnRaw;
                affected++;
            }
            catch (Exception ex)
            {
                // Resiliente: esta carteira falhou (ex.: saldo mudou), mas o run continua.
                _logger.LogError(ex, "Run demurrage: falha ao queimar de {Address} — pulando.", w.Address);
                skipped++;
            }
        }

        var run = DemurrageRun.Create(
            monthlyRateBps,
            floorRvm,
            affected,
            totalBurnRaw,
            skipped,
            request.ExecutedBy,
            preview: false);

        // CT.None: o histórico precisa ser gravado mesmo se o request HTTP já tiver caído.
        await _repo.AddAsync(run, CancellationToken.None);

        _logger.LogInformation(
            "Demurrage run concluído por {By}: {Affected} carteiras afetadas, {Skipped} isentas/falhas, {Raw} raw queimados.",
            request.ExecutedBy, affected, skipped, totalBurnRaw);

        return Result<DemurrageRunDto>.Ok(DemurrageRunDtoMapper.From(run));
    }
}
