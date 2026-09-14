using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(160).IsRequired();
        builder.Property(product => product.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value))
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.OwnsOne(product => product.Price, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
            money.Property(value => value.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
        builder.Ignore(product => product.DomainEvents);
    }
}
