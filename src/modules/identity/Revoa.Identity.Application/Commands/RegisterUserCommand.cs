using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.DTOs;

namespace Revoa.Identity.Application.Commands;

public sealed record RegisterUserCommand(
    string Nome,
    string Email,
    string Telefone,
    DateOnly BirthDate,
    string? CouponCode) : IRequest<Result<RegisterUserResult>>;
