namespace Revoa.Pricing.Application.DTOs;

// GET /api/pricing/rate — estimativa SIMBÓLICA BRL↔RVM (anônima, transparência).

/// <summary>Taxa BRL↔RVM vigente + disclaimer (RVM não é ativo financeiro).</summary>
public sealed record BrlRateDto(decimal BrlRate, string Currency, string Disclaimer);

/// <summary>POST /api/pricing/refresh — total de referências recalculadas.</summary>
public sealed record RefreshPricingResultDto(int Updated);
