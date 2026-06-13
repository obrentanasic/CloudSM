using Microsoft.EntityFrameworkCore;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Limits;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Properties;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<SmartMeter> SmartMeters => Set<SmartMeter>();

    public DbSet<ConsumptionLimit> ConsumptionLimits => Set<ConsumptionLimit>();

    public DbSet<TariffModel> TariffModels => Set<TariffModel>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
