using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Messaging;

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        string processorId,
        int batchSize,
        TimeSpan lockDuration,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
