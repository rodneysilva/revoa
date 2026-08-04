using Revoa.Abstractions;

namespace Revoa.Catalog.Domain.Aggregates.ListingAggregate;

// Localização de um anúncio (geolocalização + endereço por CEP). VO.
public class Location : ValueObject
{
    public double? Lat { get; private set; }
    public double? Lng { get; private set; }
    public string? Bairro { get; private set; }
    public string? Cidade { get; private set; }
    public string? Cep { get; private set; }

    private Location() { }

    public static Location Create(double? lat, double? lng, string? bairro, string? cidade, string? cep)
    {
        return new Location
        {
            Lat = lat,
            Lng = lng,
            Bairro = bairro,
            Cidade = cidade,
            Cep = cep
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lat;
        yield return Lng;
        yield return Bairro;
        yield return Cidade;
        yield return Cep;
    }
}
