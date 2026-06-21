using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartMetering.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> (migrations add/update). Resolves the connection
/// string in priority order: explicit env var → the WebApi appsettings(.Development).json → localdb.
/// Reading the app settings means <c>dotnet ef database update</c> targets the real (Azure) database
/// without anyone having to export a connection-string env var first.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDbFallback =
        "Server=(localdb)\\mssqllocaldb;Database=SmartMeteringDesignTime;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        // 1. Explicit env vars win (CI / scripted runs).
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__SqlDatabase")
                      ?? Environment.GetEnvironmentVariable("SqlConnectionString");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        // 2. Fall back to the WebApi app settings (ConnectionStrings:SqlDatabase).
        var fromConfig = ConnectionStringFromAppSettings();
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig!;
        }

        // 3. Last resort: localdb (lets `migrations add` work offline; `database update` would need a real server).
        return LocalDbFallback;
    }

    private static string? ConnectionStringFromAppSettings()
    {
        foreach (var path in CandidateConfigFiles())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                    && connectionStrings.TryGetProperty("SqlDatabase", out var sqlDatabase)
                    && sqlDatabase.ValueKind == JsonValueKind.String)
                {
                    var value = sqlDatabase.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed settings file — try the next candidate.
            }
        }

        return null;
    }

    /// <summary>
    /// The tool's working directory varies (repo root, /SmartMetering, or the WebApi project dir),
    /// so probe a handful of likely locations for the WebApi settings files. Development settings
    /// (which hold the real secret) take precedence over the committed appsettings.json.
    /// </summary>
    private static IEnumerable<string> CandidateConfigFiles()
    {
        var fileNames = new[] { "appsettings.Development.json", "appsettings.json" };

        var roots = new List<string>();
        var cwd = Directory.GetCurrentDirectory();
        roots.Add(cwd);
        roots.Add(Path.Combine(cwd, "WebApi"));
        roots.Add(Path.Combine(cwd, "SmartMetering", "WebApi"));

        // Walk up from the build output looking for a sibling WebApi folder.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            roots.Add(Path.Combine(dir.FullName, "WebApi"));
        }

        foreach (var root in roots)
        {
            foreach (var fileName in fileNames)
            {
                yield return Path.Combine(root, fileName);
            }
        }
    }
}
