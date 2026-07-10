using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Extensions;

public static class DatabaseProviderExtensions
{
    private const string DefaultProvider = "Sqlite";

    public static IServiceCollection AddConfiguredDbContexts(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var settings = GetDatabaseProviderSettings(configuration)
            .ResolveSqlitePath(environment.ContentRootPath);

        services.AddDbContext<AppDbContext>(
            options => ConfigureProvider(options, settings));

        services.AddDbContext<BetterAuthDbContext>(
            options => ConfigureProvider(options, settings));

        return services;
    }

    public static DatabaseProviderSettings GetDatabaseProviderSettings(IConfiguration configuration)
    {
        var configuredProvider = configuration["Database:Provider"];
        var provider = NormalizeProvider(configuredProvider);
        var connectionString = configuration.GetConnectionString(provider);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing connection string 'ConnectionStrings:{provider}' for database provider '{provider}'.");
        }

        return new DatabaseProviderSettings(provider, connectionString);
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        DatabaseProviderSettings settings)
    {
        switch (settings.Provider)
        {
            case "Sqlite":
                options.UseSqlite(settings.ConnectionString);
                break;

            case "Postgres":
                options.UseNpgsql(settings.ConnectionString);
                break;

            case "SqlServer":
                options.UseSqlServer(settings.ConnectionString);
                break;
        }
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return DefaultProvider;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "sqlite" => "Sqlite",
            "postgres" or "postgresql" or "npgsql" => "Postgres",
            "sqlserver" or "sql-server" or "mssql" => "SqlServer",
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{provider}'. Supported providers: Sqlite, Postgres, SqlServer.")
        };
    }
}

public sealed record DatabaseProviderSettings(string Provider, string ConnectionString)
{
    public DatabaseProviderSettings ResolveSqlitePath(string contentRootPath)
    {
        if (Provider != "Sqlite")
        {
            return this;
        }

        var builder = new SqliteConnectionStringBuilder(ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) ||
            builder.DataSource == ":memory:" ||
            Path.IsPathRooted(builder.DataSource))
        {
            return this;
        }

        var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = fullPath;
        return this with { ConnectionString = builder.ToString() };
    }
}
