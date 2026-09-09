using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Anvil.Extensions;

public static class DatabaseProviderExtensions
{
    private const string DefaultProvider = "Sqlite";

    /// <summary>
    /// Registers a DbContext against the configured database provider.
    /// </summary>
    /// <remarks>
    /// Generic over the context so the shared layer does not need to know which contexts
    /// a product defines. Call once per context.
    /// </remarks>
    public static IServiceCollection AddConfiguredDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
        where TContext : DbContext
    {
        var settings = GetDatabaseProviderSettings(configuration)
            .ResolveSqlitePath(environment.ContentRootPath);

        services.AddDbContext<TContext>(
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
                options.AddInterceptors(new SqlitePragmaInterceptor());
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

    // WAL lets one writer proceed concurrent with readers instead of locking the whole database
    // file for the duration of a write; a non-zero busy_timeout makes a writer wait for a
    // released lock instead of failing immediately with SQLITE_BUSY. Both matter because the
    // frontend (better-sqlite3, app/lib/db/sqlite.ts) opens its own, independent connection to
    // this same file — see openspec/specs/database-provider/spec.md, "Concurrent local writes
    // do not fail".
    public static void ApplySqlitePragmas(System.Data.Common.DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
    }
}

file sealed class SqlitePragmaInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    public override void ConnectionOpened(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
        => DatabaseProviderExtensions.ApplySqlitePragmas(connection);

    public override async Task ConnectionOpenedAsync(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        await command.ExecuteNonQueryAsync(cancellationToken);
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
