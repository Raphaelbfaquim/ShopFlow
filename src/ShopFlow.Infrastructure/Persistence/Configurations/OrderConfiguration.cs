using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Number)
            .HasConversion(number => number.Value, value => OrderNumber.Create(value))
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(order => order.Number).IsUnique();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.DiscountPolicy).HasMaxLength(40);
        builder.OwnsOne(order => order.ShippingAddress, address =>
        {
            address.Property(value => value.Street).HasColumnName("Street").HasMaxLength(160);
            address.Property(value => value.Number).HasColumnName("AddressNumber").HasMaxLength(20);
            address.Property(value => value.City).HasColumnName("City").HasMaxLength(80);
            address.Property(value => value.State).HasColumnName("State").HasMaxLength(40);
            address.Property(value => value.ZipCode).HasColumnName("ZipCode").HasMaxLength(20);
            address.Property(value => value.Country).HasColumnName("Country").HasMaxLength(40);
        });
        builder.OwnsOne(order => order.Subtotal, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Subtotal").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(order => order.Discount, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Discount").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("DiscountCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(order => order.Total, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Total").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("TotalCurrency").HasMaxLength(3);
        });
        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(order => order.DomainEvents);
        builder.HasIndex(order => order.CustomerId);
        builder.HasIndex(order => order.Status);
    }
}
