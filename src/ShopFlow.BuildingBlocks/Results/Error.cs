namespace ShopFlow.BuildingBlocks.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("Validation", message);

    public static Error NotFound(string entity, object id) =>
        new("NotFound", $"{entity} '{id}' não foi encontrado.");

    public static Error Conflict(string message) => new("Conflict", message);

    public static Error Unauthorized(string message = "Não autorizado.") =>
        new("Unauthorized", message);

    public static Error Forbidden(string message = "Acesso negado.") =>
        new("Forbidden", message);

    public static Error Unexpected(string message) => new("Unexpected", message);
}
