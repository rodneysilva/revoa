using Revoa.Demurrage.Domain.Aggregates.DemurrageRunAggregate;

namespace Revoa.Demurrage.Application.DTOs;

// Resultado de uma execução real do demurrage (run persistido em DemurrageRuns).
public sealed record DemurrageRunDto(
    Guid Id,
    DateTime RunAt,
    int RateBps,
    long FloorRvm,
    int AccountsAffected,
    string TotalBurnedRaw,
    decimal TotalBurnedRvm,
    int Skipped,
    string ExecutedBy,
    bool Preview,
    long Version);

public static class DemurrageRunDtoMapper
{
    public static DemurrageRunDto From(DemurrageRun r) => new(
        r.Id,
        r.RunAt,
        r.RateBps,
        r.FloorRvm,
        r.AccountsAffected,
        r.TotalBurnedRaw,
        RvmRawConvert.ToRvm(r.TotalBurnedRaw),
        r.Skipped,
        r.ExecutedBy,
        r.Preview,
        r.Version);
}
