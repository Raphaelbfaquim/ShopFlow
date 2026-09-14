namespace ShopFlow.BuildingBlocks.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
