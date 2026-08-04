using MediatR;
using Revoa.Abstractions;
using Revoa.Catalog.Application.DTOs;

namespace Revoa.Catalog.Application.Queries;

// Detalhe de um anúncio (GET /api/listings/{id}). Anônimo vê (UF-01).
public sealed record GetListingQuery(Guid Id) : IRequest<Result<ListingDto>>;
