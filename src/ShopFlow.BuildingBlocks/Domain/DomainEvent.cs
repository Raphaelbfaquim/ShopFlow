using ShopFlow.BuildingBlocks.Abstractions;

namespace ShopFlow.BuildingBlocks.Domain;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
