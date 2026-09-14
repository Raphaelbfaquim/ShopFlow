using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Ordering;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Persistence;

public sealed class ShopFlowDbContext : DbContext, IUnitOfWork
{
    public ShopFlowDbContext(DbContextOptions<ShopFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopFlowDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ConvertDomainEventsToOutbox()
    {
        var aggregates = ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is IAggregateRoot)
            .Select(entry => (IAggregateRoot)entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOnUtc = domainEvent.OccurredOnUtc
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}
