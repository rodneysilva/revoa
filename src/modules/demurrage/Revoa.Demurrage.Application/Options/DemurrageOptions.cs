namespace Revoa.Demurrage.Application.Options;

// Parâmetros do Demurrage (UF-27): queima periódica de uma % do RVM ocioso. Seção "Demurrage".
// MonthlyRateBps: taxa aplicada a cada execução em basis points (50 = 0,5%/mês).
// FloorRvm: piso de isenção em RVM (saldos ≤ piso não são taxados).
// Enabled: liga/desliga o módulo (preview/run retornam erro se false).
//
// Execução mensal + reajuste IPCA-trimestral (Fase 4): DemurrageSchedulerService (Infrastructure)
// roda o run no dia 1º 03:00 UTC e, em jan/abr/jul/out, reajusta a taxa RUNTIME
// (Demurrage.MonthlyRateBps no IParameterStore — visível/revertível no painel admin;
// GET /api/demurrage/ipca mostra o acumulado e a taxa ajustada).
public class DemurrageOptions
{
    public const string SectionName = "Demurrage";

    public int MonthlyRateBps { get; set; } = 50;

    public long FloorRvm { get; set; } = 100;

    public bool Enabled { get; set; } = true;
}
