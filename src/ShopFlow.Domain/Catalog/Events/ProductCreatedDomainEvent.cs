using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Catalog.Events;

public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name) : DomainEvent;
