using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeKit.Api.Migrations.Postgres;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnection =
        "Host=localhost;Database=forgekit";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                GetConnectionString(),
                provider => provider.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }

    internal static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres") ?? DefaultConnection;
}

public sealed class BetterAuthDbContextFactory : IDesignTimeDbContextFactory<BetterAuthDbContext>
{
    public BetterAuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BetterAuthDbContext>()
            .UseNpgsql(
                AppDbContextFactory.GetConnectionString(),
                provider => provider.MigrationsAssembly(typeof(BetterAuthDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new BetterAuthDbContext(options);
    }
}
