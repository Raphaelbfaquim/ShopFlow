using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Ordering.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId, string OrderNumber) : DomainEvent;
