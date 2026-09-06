using System.Text.Json;
using FluentValidation;
using Revoa.Abstractions;

namespace Revoa.Api.Middleware;

/// <summary>
/// Converte exceções não tratadas no envelope único ApiError. Sem isto, uma falha de validação
/// do pipeline (ValidationBehavior lança ValidationException) viraria 500. Produção não vaza
/// stack trace — o detalhe fica no log.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex) // FluentValidation (ValidationBehavior do pipeline CQRS)
        {
            await WriteAsync(context, 400, new ApiError(
                "Dados inválidos.",
                ex.Errors.Select(f => new FieldError(f.PropertyName, f.ErrorMessage)).ToArray()));
        }
        catch (DomainException ex) // Regras de domínio violadas (aggregates)
        {
            await WriteAsync(context, 400, new ApiError(ex.Message));
        }
        catch (ConcurrencyException)
        {
            await WriteAsync(context, 409, new ApiError(
                "Conflito de concorrência: o registro foi alterado por outra operação. Tente novamente."));
        }
        catch (DuplicateKeyException ex)
        {
            await WriteAsync(context, 409, new ApiError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado em {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, 500, new ApiError("Erro interno."));
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, ApiError error)
    {
        if (context.Response.HasStarted)
        {
            return; // Resposta já começou a ir ao fio — não há como trocar o corpo.
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
}
