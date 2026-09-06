using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

// Localização de um anúncio (geolocalização + endereço por CEP). VO.
public class Location : ValueObject
{
    public double? Lat { get; private set; }
    public double? Lng { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }

    private Location() { }

    public static Location Create(double? lat, double? lng, string? bairro, string? cidade, string? cep)
    {
        return new Location
        {
            Lat = lat,
            Lng = lng,
            Neighborhood = bairro,
            City = cidade,
            PostalCode = cep
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lat;
        yield return Lng;
        yield return Neighborhood;
        yield return City;
        yield return PostalCode;
    }
}
