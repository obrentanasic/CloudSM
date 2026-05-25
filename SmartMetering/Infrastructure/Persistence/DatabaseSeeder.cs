using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    /// <summary>Creates an initial active Admin from config if none with that email exists. Idempotent.</summary>
    public static async Task SeedAdminAsync(IServiceProvider services, AdminSeedOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DatabaseSeeder));

        var email = options.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return;
        }

        var admin = User.Create(options.FirstName, options.LastName, email, options.PhoneNumber, UserRole.Admin);
        admin.SetPassword(hasher.Hash(options.Password));

        await db.Users.AddAsync(admin, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded initial admin user: {Email}", email);
    }
}
