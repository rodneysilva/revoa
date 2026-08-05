using System.Text.Json;
using MediatR;
using Revoa.Abstractions;
using Revoa.IntegrationContracts.Admin;

namespace Revoa.Admin.Application.Commands;

// Altera um parâmetro de sistema em runtime (UF-30, Admin). SEGURANÇA: a Key DEVE estar na
// whitelist de chaves conhecidas — só estes parâmetros podem ser alterados via API. O Value é
// recebido como JsonElement (aceita string/number/bool); é serializado e gravado no store. O tipo
// é inferido do próprio valor enviado (não há coerção aqui). UpdatedBy vem da claim de e-mail do
// admin autenticado (definida no controller), nunca do body.
public sealed record SetParameterCommand(string Key, JsonElement Value, string UpdatedBy)
    : IRequest<Result>;

public class SetParameterCommandHandler : IRequestHandler<SetParameterCommand, Result>
{
    // Whitelist obrigatória: só chaves conhecidas podem ser gravadas (evita injeção de parâmetros
    // arbitrários no store). Deve espelhar o catálogo do GetAllParametersQuery.
    private static readonly HashSet<string> AllowedKeys = new()
    {
        "DonationReward.BonusRvm",
        "DonationReward.ReputationPoints",
        "DonationReward.HelpPoints",
        "Demurrage.MonthlyRateBps",
        "Demurrage.FloorRvm",
        "Demurrage.Enabled",
        "Pricing.BrlRate",
        "Pricing.UseOllama"
    };

    private readonly IParameterStore _store;

    public SetParameterCommandHandler(IParameterStore store)
    {
        _store = store;
    }

    public async Task<Result> Handle(SetParameterCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || !AllowedKeys.Contains(request.Key))
        {
            return Result.Fail("Parâmetro desconhecido.");
        }

        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return Result.Fail("UpdatedBy é obrigatório.");
        }

        // Aceita apenas valores primitivos (number/string/bool). Rejeita object/array/null.
        var kind = request.Value.ValueKind;
        if (kind is JsonValueKind.Object or JsonValueKind.Array
            or JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Result.Fail("Valor inválido para o parâmetro.");
        }

        await _store.SetAsync(request.Key, request.Value, request.UpdatedBy, ct);
        return Result.Ok();
    }
}
