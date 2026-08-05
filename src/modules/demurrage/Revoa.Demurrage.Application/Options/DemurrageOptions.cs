namespace Revoa.Demurrage.Application.Options;

// Parâmetros do Demurrage (UF-27): queima periódica de uma % do RVM ocioso. Seção "Demurrage".
// MonthlyRateBps: taxa aplicada a cada execução em basis points (50 = 0,5%/mês).
// FloorRvm: piso de isenção em RVM (saldos ≤ piso não são taxados).
// Enabled: liga/desliga o módulo (preview/run retornam erro se false).
//
// TODO (Fase 4): reajuste IPCA-trimestral automático da taxa + scheduler Quartz (execução mensal).
public class DemurrageOptions
{
    public const string SectionName = "Demurrage";

    public int MonthlyRateBps { get; set; } = 50;

    public long FloorRvm { get; set; } = 100;

    public bool Enabled { get; set; } = true;
}
