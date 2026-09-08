namespace Revoa.Demurrage.Application.Services;

// Reajuste da taxa mensal pelo IPCA acumulado no trimestre. Único ponto com a regra — o
// scheduler trimestral e o GET /api/demurrage/ipca derivam do MESMO cálculo, então o admin
// vê exatamente o que seria aplicado. Clamps anti-excesso: piso 10 bps (0,1%/mês),
// teto 500 bps (5%/mês).
public static class DemurrageRateAdjuster
{
    public const int MinBps = 10;
    public const int MaxBps = 500;

    public static int Apply(int currentBps, double ipcaAccumulatedPercent)
    {
        var adjusted = currentBps * (1 + ipcaAccumulatedPercent / 100.0);
        return Math.Clamp((int)Math.Round(adjusted), MinBps, MaxBps);
    }
}
