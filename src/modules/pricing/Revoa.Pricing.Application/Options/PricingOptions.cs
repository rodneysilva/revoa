namespace Revoa.Pricing.Application.Options;

// Parâmetros admin-configuráveis do Pricing Intelligence (UF-28). Seção "Pricing" do appsettings.
// BrlRate: taxa global de referência BRL (dev: 1.0 = 1 RVM ≈ R$1). BrlReferences: override por
// slug de categoria (slug → referência BRL absoluta em R$). Ollama/IBGE: endpoints públicos.
public class PricingOptions
{
    public const string SectionName = "Pricing";

    // Taxa global BRL (conversão RVM↔R$ em dev).
    public decimal BrlRate { get; set; } = 1.0m;

    // Referência BRL absoluta por categoria (slug → R$). Opcional — fallback global.
    public Dictionary<string, decimal>? BrlReferences { get; set; }

    // Ollama (LLM local para refinar a sugestão justa).
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "qwen2.5:7b";
    public bool UseOllama { get; set; } = true;

    // IPCA IBGE (variação mensal, últimos 12 meses, Brasil).
    public string IbgeUrl { get; set; } =
        "https://servicodados.ibge.gov.br/api/v3/agregados/1737/periodos/-12/variaveis/63?localidades=N1[all]";
}
