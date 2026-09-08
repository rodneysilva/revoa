namespace Revoa.Demurrage.Application.Services;

// Agenda do demurrage: run mensal no dia 1º às 03:00 UTC (hora de baixo uso no Brasil);
// reajuste IPCA nos meses de fechamento trimestral. Classe pura/pública — os testes cravam
// a aritmética de datas, e o scheduler + a query de status usam a mesma fonte.
public static class DemurrageSchedule
{
    public const int RunHourUtc = 3;

    // Fechamento trimestral: o IPCA do trimestre fecha (e o dado sai) em jan/abr/jul/out.
    public static bool IsQuarterStart(int utcMonth) => utcMonth is 1 or 4 or 7 or 10;

    public static DateTimeOffset NextMonthlyRunUtc(DateTimeOffset now)
    {
        var candidate = new DateTime(
            now.UtcDateTime.Year, now.UtcDateTime.Month, 1, RunHourUtc, 0, 0, DateTimeKind.Utc);
        if (candidate <= now.UtcDateTime)
        {
            candidate = candidate.AddMonths(1);
        }

        return new DateTimeOffset(candidate, TimeSpan.Zero);
    }
}
