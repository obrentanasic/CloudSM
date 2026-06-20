using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.ManualReadings;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class ManualReadingConfiguration : IEntityTypeConfiguration<ManualReading>
{
    public void Configure(EntityTypeBuilder<ManualReading> builder)
    {
        builder.ToTable("ManualReadings");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(r => r.MeterId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(r => r.ConsumerId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(r => r.SerialNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.DeclaredTotalEnergyKwh).HasPrecision(18, 3);
        builder.Property(r => r.Note).HasMaxLength(500);
        builder.Property(r => r.OriginalImageBlobName).HasMaxLength(400).IsRequired();
        builder.Property(r => r.OptimizedImageBlobName).HasMaxLength(400);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(r => r.ReviewedByUserId)
            .HasConversion(id => id!.Value.Value, value => EntityId.From(value));
        builder.Property(r => r.ReviewNote).HasMaxLength(500);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.MeterId, r.Status });
        builder.HasIndex(r => r.ConsumerId);

        builder.HasOne<SmartMeter>()
            .WithMany()
            .HasForeignKey(r => r.MeterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(r => r.DomainEvents);
    }
}
