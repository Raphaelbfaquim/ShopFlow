using Microsoft.AspNetCore.Diagnostics;
using ShopFlow.Domain.Exceptions;

namespace ShopFlow.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Erro não tratado.");

        var status = exception switch
        {
            DomainException => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        await Results.Problem(
            title: exception is DomainException domain ? domain.Code : exception.GetType().Name,
            detail: exception.Message,
            statusCode: status).ExecuteAsync(httpContext);

        return true;
    }
}
