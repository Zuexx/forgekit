using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeKit.Api.Migrations.Sqlite;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(
                DesignTimeSqliteConnection.Create(),
                provider => provider.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}

public sealed class BetterAuthDbContextFactory : IDesignTimeDbContextFactory<BetterAuthDbContext>
{
    public BetterAuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BetterAuthDbContext>()
            .UseSqlite(
                DesignTimeSqliteConnection.Create(),
                provider => provider.MigrationsAssembly(typeof(BetterAuthDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new BetterAuthDbContext(options);
    }
}

file static class DesignTimeSqliteConnection
{
    public static string Create()
    {
        var apiDirectory = FindApiDirectory();
        var dataDirectory = Path.Combine(apiDirectory, "data");
        Directory.CreateDirectory(dataDirectory);
        return $"Data Source={Path.Combine(dataDirectory, "forgekit.db")}";
    }

    private static string FindApiDirectory()
    {
        var roots = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var root in roots)
        {
            for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            {
                var candidates = new[]
                {
                    directory.FullName,
                    Path.Combine(directory.FullName, "ForgeKit.Api"),
                    Path.Combine(directory.FullName, "api", "ForgeKit.Api")
                };

                var match = candidates.FirstOrDefault(
                    path => File.Exists(Path.Combine(path, "ForgeKit.Api.csproj")));
                if (match is not null)
                {
                    return match;
                }
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate ForgeKit.Api from the current or assembly directory.");
    }
}
