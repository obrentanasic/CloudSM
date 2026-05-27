using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Limits;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class ConsumptionLimitConfiguration : IEntityTypeConfiguration<ConsumptionLimit>
{
    public void Configure(EntityTypeBuilder<ConsumptionLimit> builder)
    {
        builder.ToTable("ConsumptionLimits");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(l => l.UserId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();
        builder.HasIndex(l => l.UserId).IsUnique();

        builder.Property(l => l.Value).HasPrecision(18, 3);
        builder.Property(l => l.Unit).HasConversion<string>().HasMaxLength(10);
        builder.Property(l => l.LastAlertedMonth).HasMaxLength(7);

        builder.Ignore(l => l.DomainEvents);
    }
}
