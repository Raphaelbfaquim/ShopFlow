using ShopFlow.BuildingBlocks.Abstractions;

namespace ShopFlow.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
