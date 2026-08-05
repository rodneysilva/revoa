using System.Globalization;
using System.Numerics;

namespace Revoa.Demurrage.Application.DTOs;

// Pré-visualização do demurrage (cálculo SEM queimar). RateBps/Floor aplicados; contagem de
// afetados/isentos; total queimado em raw (string decimal) e em RVM (decimal p/ exibição).
public sealed record DemurragePreviewDto(
    int RateBps,
    long FloorRvm,
    int AccountsAffected,
    int Skipped,
    string TotalBurnedRaw,
    decimal TotalBurnedRvm);

// Helper de conversão raw(18d) -> RVM (decimal). Safe p/ magnitude: se o raw exceder decimal,
// usa divisão inteira (unidades cheias de RVM). Para volumes realistas do revoa, decimal basta.
public static class RvmRawConvert
{
    private static readonly BigInteger Unit = BigInteger.Pow(10, 18);
    private static readonly BigInteger DecimalMax = new(decimal.MaxValue);

    public static decimal ToRvm(BigInteger raw)
    {
        if (raw >= DecimalMax)
        {
            return (decimal)(raw / Unit);
        }
        return (decimal)raw / 1_000_000_000_000_000_000m;
    }

    public static decimal ToRvm(string rawText)
    {
        return BigInteger.TryParse(rawText ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw)
            ? ToRvm(raw)
            : 0m;
    }
}
