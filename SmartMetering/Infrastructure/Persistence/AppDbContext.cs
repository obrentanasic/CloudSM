using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
