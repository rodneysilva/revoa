using System.Text.Json.Serialization;

namespace Revoa.Abstractions;

/// <summary>
/// Formato ÚNICO de erro da API (PascalCase). Controllers devolvem <c>new ApiError(msg)</c>;
/// o ApiExceptionMiddleware mapeia exceções (validação/concorrência/duplicidade) para o mesmo
/// shape — um só contrato de erro para o frontend.
/// </summary>
public sealed record ApiError(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    FieldError[]? Errors = null);

/// <summary>Erro de campo individual (falhas de validação do pipeline FluentValidation).</summary>
public sealed record FieldError(string Field, string Message);

/// <summary>
/// Corpo de sucesso mínimo: identificador do recurso criado/afetado. Aceita Guid ou string
/// (handlers que devolvem Result&lt;string&gt; com o id).
/// </summary>
public sealed record ResourceId(Guid Id)
{
    public ResourceId(string id) : this(Guid.Parse(id)) { }
}

/// <summary>Mensagem informativa neutra (ex.: respostas anti-enumeração de login).</summary>
public sealed record ApiMessage(string Message);
