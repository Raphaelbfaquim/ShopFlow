using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Name).HasMaxLength(120).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Role).HasMaxLength(32).IsRequired();
        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasMaxLength(180)
            .IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Ignore(user => user.DomainEvents);
    }
}
