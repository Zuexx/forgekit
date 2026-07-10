using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeKit.Api.Migrations.SqlServer;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnection =
        "Server=localhost;Database=forgekit;Integrated Security=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                GetConnectionString(),
                provider => provider.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }

    internal static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") ?? DefaultConnection;
}

public sealed class BetterAuthDbContextFactory : IDesignTimeDbContextFactory<BetterAuthDbContext>
{
    public BetterAuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BetterAuthDbContext>()
            .UseSqlServer(
                AppDbContextFactory.GetConnectionString(),
                provider => provider.MigrationsAssembly(typeof(BetterAuthDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new BetterAuthDbContext(options);
    }
}
