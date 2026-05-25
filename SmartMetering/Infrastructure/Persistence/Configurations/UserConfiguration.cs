using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PhoneNumber).HasMaxLength(32);

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.SecurityToken).HasMaxLength(128);
        builder.HasIndex(u => u.SecurityToken);

        builder.Ignore(u => u.DomainEvents);
    }
}
