using ShopFlow.Infrastructure.Messaging;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Tests.Messaging;

public sealed class ConcurrentOutboxStore : IOutboxStore
{
    private readonly object _gate = new();
    private readonly List<OutboxMessage> _messages;

    public ConcurrentOutboxStore(params OutboxMessage[] messages)
    {
        _messages = [.. messages];
    }

    public IReadOnlyList<OutboxMessage> Messages => _messages;

    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        string processorId,
        int batchSize,
        TimeSpan lockDuration,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var claimed = _messages
                .Where(message => message.CanClaim(utcNow))
                .OrderBy(message => message.OccurredOnUtc)
                .Take(batchSize)
                .ToList();

            foreach (var message in claimed)
            {
                message.Claim(processorId, utcNow.Add(lockDuration));
            }

            return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
        }
    }

    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_messages.Contains(message))
            {
                _messages.Add(message);
            }
        }

        return Task.CompletedTask;
    }
}
