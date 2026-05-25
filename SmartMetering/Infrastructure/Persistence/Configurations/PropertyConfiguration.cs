using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(250).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.Property(p => p.OwnerId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();
        builder.HasIndex(p => p.OwnerId);

        builder.Ignore(p => p.DomainEvents);
    }
}
