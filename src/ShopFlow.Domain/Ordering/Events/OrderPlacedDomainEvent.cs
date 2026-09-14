using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Ordering.Events;

public sealed record OrderPlacedDomainEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency) : DomainEvent;
