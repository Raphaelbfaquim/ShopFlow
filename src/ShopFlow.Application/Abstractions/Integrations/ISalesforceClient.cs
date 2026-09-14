namespace ShopFlow.Application.Abstractions.Integrations;

public sealed record SalesforceOrderPayload(
    string OrderNumber,
    string CustomerEmail,
    decimal Total,
    string Currency,
    IReadOnlyList<SalesforceOrderLine> Lines);

public sealed record SalesforceOrderLine(string Sku, int Quantity, decimal UnitPrice);

public interface ISalesforceClient
{
    Task SyncOrderAsync(SalesforceOrderPayload payload, CancellationToken cancellationToken = default);
}
