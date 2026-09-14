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
        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}
