using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class SmartMeterConfiguration : IEntityTypeConfiguration<SmartMeter>
{
    public void Configure(EntityTypeBuilder<SmartMeter> builder)
    {
        builder.ToTable("SmartMeters");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(m => m.SerialNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(m => m.SerialNumber).IsUnique();

        builder.Property(m => m.ConnectionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MaxApprovedPowerKw).HasPrecision(6, 2);
        builder.Property(m => m.Note).HasMaxLength(500);
        builder.Property(m => m.PairingStatus).HasConversion<string>().HasMaxLength(20);

        builder.Property(m => m.DeviceUuid).HasMaxLength(64);
        builder.HasIndex(m => m.DeviceUuid);
        builder.Property(m => m.DeviceAccessToken).HasMaxLength(128);
        builder.HasIndex(m => m.DeviceAccessToken);

        builder.Property(m => m.PropertyId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(m => m.DomainEvents);
    }
}
