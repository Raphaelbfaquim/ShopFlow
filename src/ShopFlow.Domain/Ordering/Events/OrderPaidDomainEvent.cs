using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Ordering.Events;

public sealed record OrderPaidDomainEvent(Guid OrderId, string OrderNumber, decimal TotalAmount) : DomainEvent;
