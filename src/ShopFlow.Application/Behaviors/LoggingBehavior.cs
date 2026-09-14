using MediatR;
using Microsoft.Extensions.Logging;

namespace ShopFlow.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILoggingAdapter _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = new LoggerAdapter(logger);
    }

    internal LoggingBehavior(ILoggingAdapter logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", name);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", name);
        return response;
    }

    public interface ILoggingAdapter
    {
        void LogInformation(string message, params object[] args);
    }

    private sealed class LoggerAdapter : ILoggingAdapter
    {
        private readonly ILogger _logger;

        public LoggerAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args) =>
            _logger.LogInformation(message, args);
    }
}
