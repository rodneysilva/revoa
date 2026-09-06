using MediatR;
using Revoa.Abstractions;
using Revoa.Identity.Application.DTOs;

namespace Revoa.Identity.Application.Commands;

public sealed record RegisterUserCommand(
    string Name,
    string Email,
    string Phone,
    DateOnly BirthDate,
    string? CouponCode) : IRequest<Result<RegisterUserResult>>;
