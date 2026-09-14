using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(512).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.Error).HasMaxLength(2000);
        builder.Property(message => message.LockedBy).HasMaxLength(120);
        builder.Property(message => message.Version).IsConcurrencyToken();
        builder.Ignore(message => message.IsProcessed);
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.LockedUntilUtc });
    }
}
