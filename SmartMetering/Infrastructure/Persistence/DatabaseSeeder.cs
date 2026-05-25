using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static Task SeedAdminAsync(IServiceProvider services, UserSeedOptions options, CancellationToken ct = default) =>
        SeedUserAsync(services, options, UserRole.Admin, ct);

    /// <summary>Dev-only convenience: a ready-to-login Consumer so the consumer endpoints can be tested without the email flow.</summary>
    public static Task SeedConsumerAsync(IServiceProvider services, UserSeedOptions options, CancellationToken ct = default) =>
        SeedUserAsync(services, options, UserRole.Consumer, ct);

    private static async Task SeedUserAsync(IServiceProvider services, UserSeedOptions options, UserRole role, CancellationToken ct)
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

        var user = User.Create(options.FirstName, options.LastName, email, options.PhoneNumber, role);
        user.SetPassword(hasher.Hash(options.Password));

        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded {Role} user: {Email}", role, email);
    }
}
