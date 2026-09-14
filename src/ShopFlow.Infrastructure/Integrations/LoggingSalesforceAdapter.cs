using Microsoft.Extensions.Logging;
using ShopFlow.Application.Abstractions.Integrations;

namespace ShopFlow.Infrastructure.Integrations;

public sealed class LoggingSalesforceAdapter : ISalesforceClient
{
    private readonly ILogger<LoggingSalesforceAdapter> _logger;

    public LoggingSalesforceAdapter(ILogger<LoggingSalesforceAdapter> logger)
    {
        _logger = logger;
    }

    public Task SyncOrderAsync(SalesforceOrderPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Adapter Salesforce (local): pedido {OrderNumber} total {Total} {Currency} com {Lines} itens.",
            payload.OrderNumber,
            payload.Total,
            payload.Currency,
            payload.Lines.Count);
        return Task.CompletedTask;
    }
}
