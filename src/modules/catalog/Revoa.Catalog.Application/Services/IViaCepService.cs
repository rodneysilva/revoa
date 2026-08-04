namespace Revoa.Catalog.Application.Services;

// Resultado da consulta ViaCEP (bairro/cidade/estado por CEP).
public record ViaCepAddress(string? Bairro, string? Cidade, string? Estado);

// Porta para o ViaCEP (https://viacep.com.br). Implementação HttpClient no Infrastructure.
public interface IViaCepService
{
    Task<ViaCepAddress?> GetByCepAsync(string cep, CancellationToken ct = default);
}
