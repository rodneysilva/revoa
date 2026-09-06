using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Revoa.Abstractions;
using Revoa.Admin.Application.Commands;
using Revoa.Admin.Application.DTOs;
using Revoa.Admin.Application.Queries;
using Revoa.Identity.Application.Commands;
using Revoa.Identity.Domain.Aggregates.UserAggregate;

namespace Revoa.Api.Controllers;

// Admin unificado (UF-30, Fase 3): controle runtime de TODAS as parametrizações do sistema.
// Tudo gated Admin (claim email em Admin:Emails). Os demais endpoints admin (cupons, denúncias,
// pricing, demurrage, seed) seguem em seus controllers e são LINKADOS no dashboard do front.
//
// Os parâmetros alterados aqui passam a valer IMEDIATAMENTE (sem restart): cada módulo lê do
// IParameterStore com fallback para os defaults do IOptions/appsettings.
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Lista todos os parâmetros conhecidos (rótulo/tipo pt-BR + valor efetivo atual).
    [HttpGet("parameters")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ParameterDto>>> GetParameters(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllParametersQuery(), ct);
        return result.IsFailure
            ? BadRequest(new ApiError(result.Error))
            : Ok(result.Value);
    }

    // Altera um parâmetro runtime. Body { Value } (string/number/bool). updatedBy = e-mail do admin.
    [HttpPut("parameters/{key}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> SetParameter(string key, [FromBody] SetParameterRequest? body, CancellationToken ct)
    {
        if (body is null || body.Value.ValueKind == JsonValueKind.Undefined)
        {
            return BadRequest(new ApiError("Corpo inválido: informe { \"Value\": ... }."));
        }

        var updatedBy = User.FindFirst("email")?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? "admin";

        var result = await _mediator.Send(new SetParameterCommand(key, body.Value, updatedBy), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }

    // Altera a role de um usuário (User/Mod/Admin/Arbitrator). Única via de escrita de UserRole;
    // alimenta a claim "role" do próximo JWT (policy "Arbitrator" do resolve de disputas).
    [HttpPut("users/{id:guid}/role")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> SetUserRole(Guid id, [FromBody] SetUserRoleRequest body, CancellationToken ct)
    {
        var updatedBy = User.FindFirst("email")?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? "admin";

        var result = await _mediator.Send(new SetUserRoleCommand(id, body.Role, updatedBy), ct);
        return result.IsFailure ? BadRequest(new ApiError(result.Error)) : NoContent();
    }
}

// Body do PUT /api/admin/users/{id}/role. Role = nome do enum (bind via JsonStringEnumConverter).
public sealed record SetUserRoleRequest(UserRole Role);

// Body do PUT /api/admin/parameters/{key}. Value é o valor cru (number/bool/string).
public sealed record SetParameterRequest(JsonElement Value);
