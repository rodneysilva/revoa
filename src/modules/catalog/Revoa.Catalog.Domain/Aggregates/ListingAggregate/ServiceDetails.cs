using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

// VO tipado para kind=Service: UnitType, Duration, VoucherExpiry.
// Voucher é mintado APENAS na compra (mint-on-purchase, UF-13) — não ao listar.
public enum ServiceUnitType
{
    PerService,
    Hours
}

public class ServiceDetails : ValueObject
{
    public ServiceUnitType UnitType { get; private set; }
    public int Duration { get; private set; }
    public int VoucherExpiryDays { get; private set; }

    private ServiceDetails() { }

    public static ServiceDetails Create(ServiceUnitType unitType, int duration, int voucherExpiryDays)
    {
        if (duration < 0)
        {
            throw new DomainException("Duração não pode ser negativa.");
        }

        if (voucherExpiryDays <= 0)
        {
            voucherExpiryDays = 30; // default 30d
        }

        return new ServiceDetails { UnitType = unitType, Duration = duration, VoucherExpiryDays = voucherExpiryDays };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UnitType;
        yield return Duration;
        yield return VoucherExpiryDays;
    }
}
