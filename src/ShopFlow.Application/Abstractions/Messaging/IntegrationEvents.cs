namespace ShopFlow.Application.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTime OccurredOnUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}
