using Microsoft.EntityFrameworkCore;
using ShopFlow.Infrastructure.Persistence;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class EntityFrameworkOutboxStore : IOutboxStore
{
    private readonly ShopFlowDbContext _dbContext;

    public EntityFrameworkOutboxStore(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        string processorId,
        int batchSize,
        TimeSpan lockDuration,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null
                && (message.LockedUntilUtc == null || message.LockedUntilUtc < utcNow))
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return pending;
        }

        var lockedUntil = utcNow.Add(lockDuration);
        foreach (var message in pending)
        {
            message.Claim(processorId, lockedUntil);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return pending;
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries<OutboxMessage>())
            {
                entry.State = EntityState.Detached;
            }

            return [];
        }
    }

    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
