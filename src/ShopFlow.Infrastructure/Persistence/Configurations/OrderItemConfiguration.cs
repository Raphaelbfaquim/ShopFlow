using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Domain.Ordering;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductName).HasMaxLength(160).IsRequired();
        builder.Property(item => item.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value))
            .HasMaxLength(32)
            .IsRequired();
        builder.OwnsOne(item => item.UnitPrice, money =>
        {
            money.Property(value => value.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("UnitCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(item => item.LineTotal, money =>
        {
            money.Property(value => value.Amount).HasColumnName("LineTotal").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("LineCurrency").HasMaxLength(3);
        });
    }
}
