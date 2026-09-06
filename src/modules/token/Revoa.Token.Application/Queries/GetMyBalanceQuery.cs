using System.Numerics;
using MediatR;
using Microsoft.Extensions.Logging;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Accounts;
using Revoa.Token.Application.Services;
using Revoa.Token.Domain;

namespace Revoa.Token.Application.Queries;

// Saldo RVM do usuário logado (GET /api/wallet/balance). Lê o endereço via port
// IWalletAddressReader (Account é dono da carteira; Token consome o port) e o saldo
// on-chain via IRvmService.balanceOf (raw 18 decimais → RVM).
//
// Degrada sem erro: sem carteira criada OU chain inacessível → Rvm = null (o FE
// simplesmente não exibe o chip de saldo; nunca bloqueia a UI).
public sealed record GetMyBalanceQuery(Guid UserId) : IRequest<Result<WalletBalanceDto>>;

public sealed record WalletBalanceDto(string? WalletAddress, decimal? Rvm);

public class GetMyBalanceQueryHandler
    : IRequestHandler<GetMyBalanceQuery, Result<WalletBalanceDto>>
{
    private static readonly BigInteger Unit = BigInteger.Pow(10, RvmConstants.Decimals);

    private readonly IRvmService _rvm;
    private readonly IWalletAddressReader _wallets;
    private readonly ILogger<GetMyBalanceQueryHandler> _logger;

    public GetMyBalanceQueryHandler(
        IRvmService rvm,
        IWalletAddressReader wallets,
        ILogger<GetMyBalanceQueryHandler> logger)
    {
        _rvm = rvm;
        _wallets = wallets;
        _logger = logger;
    }

    public async Task<Result<WalletBalanceDto>> Handle(GetMyBalanceQuery request, CancellationToken ct)
    {
        var wallet = await _wallets.GetByUserIdAsync(request.UserId, ct);
        if (wallet is null)
        {
            // Conta ainda sem carteira (cadeia de eventos de registro não rodou / EOA pendente).
            return Result<WalletBalanceDto>.Ok(new WalletBalanceDto(null, null));
        }

        try
        {
            var raw = await _rvm.BalanceOfAsync(wallet.Address, ct);
            var whole = BigInteger.DivRem(raw, Unit, out var rem);
            var rvm = Math.Round((decimal)whole + (decimal)rem / (decimal)Unit, 4);
            return Result<WalletBalanceDto>.Ok(new WalletBalanceDto(wallet.Address, rvm));
        }
        catch (Exception ex)
        {
            // Chain fora do ar (ex.: anvil down): saldo indisponível, não é erro do usuário.
            _logger.LogWarning(ex, "Falha ao ler saldo RVM de {Address}", wallet.Address);
            return Result<WalletBalanceDto>.Ok(new WalletBalanceDto(wallet.Address, null));
        }
    }
}
