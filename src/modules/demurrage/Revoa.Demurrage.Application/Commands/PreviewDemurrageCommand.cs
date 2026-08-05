using System.Globalization;
using System.Numerics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Revoa.Abstractions;
using Revoa.Demurrage.Application.DTOs;
using Revoa.Demurrage.Application.Options;
using Revoa.IntegrationContracts.Accounts;
using Revoa.Token.Application.Services;
using Revoa.Token.Domain;

namespace Revoa.Demurrage.Application.Commands;

// Pré-visualização do demurrage (Admin): calcula quanto seria queimado SEM executar on-chain.
// Para cada carteira: lê o saldo (raw 18d); se o saldo inteiro em RVM (bal/10^18) ≤ Floor -> isenta;
// senão burnRaw = bal * RateBps / 10000. Acumula total/afetados/isentos. Resiliente: falha ao ler
// um saldo apenas pula a carteira. Preview NÃO garante BURNER_ROLE nem persiste (só calcula).
public sealed record PreviewDemurrageCommand : IRequest<Result<DemurragePreviewDto>>;

public class PreviewDemurrageCommandHandler
    : IRequestHandler<PreviewDemurrageCommand, Result<DemurragePreviewDto>>
{
    private static readonly BigInteger Unit = BigInteger.Pow(10, RvmConstants.Decimals);

    private readonly IRvmService _rvm;
    private readonly IWalletAddressReader _wallets;
    private readonly DemurrageOptions _options;
    private readonly ILogger<PreviewDemurrageCommandHandler> _logger;

    public PreviewDemurrageCommandHandler(
        IRvmService rvm,
        IWalletAddressReader wallets,
        IOptions<DemurrageOptions> options,
        ILogger<PreviewDemurrageCommandHandler> logger)
    {
        _rvm = rvm;
        _wallets = wallets;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<DemurragePreviewDto>> Handle(PreviewDemurrageCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return Result<DemurragePreviewDto>.Fail("Demurrage está desativado (Demurrage:Enabled=false).");
        }

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
                _logger.LogWarning(ex, "Preview demurrage: falha ao ler saldo de {Address} — pulando.", w.Address);
                skipped++;
                continue;
            }

            // Saldo em RVM (inteiro) <= piso -> isento (não taxa).
            if (balance / Unit <= _options.FloorRvm)
            {
                skipped++;
                continue;
            }

            // burnRaw = saldo * taxa / 10000 (basis points). Base é o saldo total, não (saldo - piso).
            var burnRaw = balance * _options.MonthlyRateBps / 10000;
            if (burnRaw <= 0)
            {
                continue;
            }

            totalBurnRaw += burnRaw;
            affected++;
        }

        return Result<DemurragePreviewDto>.Ok(new DemurragePreviewDto(
            _options.MonthlyRateBps,
            _options.FloorRvm,
            affected,
            skipped,
            totalBurnRaw.ToString(CultureInfo.InvariantCulture),
            RvmRawConvert.ToRvm(totalBurnRaw)));
    }
}
