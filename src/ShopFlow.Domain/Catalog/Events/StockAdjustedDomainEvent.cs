using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Catalog.Events;

public sealed record StockAdjustedDomainEvent(Guid ProductId, int Quantity, int CurrentStock) : DomainEvent;
