namespace ShopFlow.BuildingBlocks.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
