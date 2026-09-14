using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ShopFlow.Application.Abstractions.Integrations;

namespace ShopFlow.Infrastructure.Integrations;

public sealed class SalesforceHttpAdapter : ISalesforceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SalesforceHttpAdapter> _logger;

    public SalesforceHttpAdapter(HttpClient httpClient, ILogger<SalesforceHttpAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SyncOrderAsync(SalesforceOrderPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("services/data/v59.0/sobjects/Order", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Falha ao sincronizar pedido {OrderNumber} com Salesforce: {Status} {Body}", payload.OrderNumber, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Pedido {OrderNumber} sincronizado com Salesforce.", payload.OrderNumber);
    }
}
