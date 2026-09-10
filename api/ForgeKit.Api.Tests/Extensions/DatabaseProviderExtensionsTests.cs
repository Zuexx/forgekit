using Anvil.Extensions;
using ForgeKit.Api.Data;
using ForgeKit.Api.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ForgeKit.Api.Tests.Extensions;

public sealed class DatabaseProviderExtensionsTests
{
    [Theory]
    [InlineData(null, "Sqlite")]
    [InlineData("Sqlite", "Sqlite")]
    [InlineData("SQLite", "Sqlite")]
    [InlineData("Postgres", "Postgres")]
    [InlineData("PostgreSQL", "Postgres")]
    [InlineData("Npgsql", "Postgres")]
    [InlineData("SqlServer", "SqlServer")]
    [InlineData("MSSQL", "SqlServer")]
    public void GetDatabaseProviderSettings_ShouldResolveSupportedProviders(
        string? configuredProvider,
        string expectedProvider)
    {
        var configuration = CreateConfiguration(configuredProvider);

        var settings = DatabaseProviderExtensions.GetDatabaseProviderSettings(configuration);

        settings.Provider.ShouldBe(expectedProvider);
        settings.ConnectionString.ShouldBe($"{expectedProvider}-connection");
    }

    [Fact]
    public void GetDatabaseProviderSettings_ShouldRejectUnsupportedProvider()
    {
        var configuration = CreateConfiguration("Oracle");

        var act = () => DatabaseProviderExtensions.GetDatabaseProviderSettings(configuration);

        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Unsupported database provider");
    }

    [Fact]
    public void GetDatabaseProviderSettings_ShouldRejectMissingProviderConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres"
            })
            .Build();

        var act = () => DatabaseProviderExtensions.GetDatabaseProviderSettings(configuration);

        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("ConnectionStrings:Postgres");
    }

    [Fact]
    public void ResolveSqlitePath_ShouldResolveRelativeFileUnderContentRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new DatabaseProviderSettings("Sqlite", "Data Source=./data/test.db");

        var resolved = settings.ResolveSqlitePath(root);

        resolved.ConnectionString.ShouldContain(Path.Combine(root, "data", "test.db"));
        Directory.Exists(Path.Combine(root, "data")).ShouldBeTrue();
    }

    [Fact]
    public void ResolveSqlitePath_DefaultConnectionString_ResolvesToRepoRootDataDirectory()
    {
        // Mirrors the real layout: content root is api/ForgeKit.Api, two levels up is the repo
        // root. This is the exact connection string appsettings.json ships — a regression here
        // means the API and the frontend stop agreeing on where the shared file lives.
        var repoRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var contentRoot = Path.Combine(repoRoot, "api", "ForgeKit.Api");
        Directory.CreateDirectory(contentRoot);
        var settings = new DatabaseProviderSettings("Sqlite", "Data Source=../../data/forgekit.db");

        var resolved = settings.ResolveSqlitePath(contentRoot);

        resolved.ConnectionString.ShouldContain(Path.Combine(repoRoot, "data", "forgekit.db"));
    }

    [Fact]
    public async Task AddConfiguredDbContext_ForSqlite_WiresThePragmaInterceptor()
    {
        // Regression guard for the interceptor registration itself, not just the pragma
        // function it calls: SqlitePragmaInterceptorTests proves ApplySqlitePragmas works when
        // called directly, but nothing else proved options.AddInterceptors(...) in
        // ConfigureProvider's Sqlite case is actually reached through the real
        // AddConfiguredDbContext path production code uses. A previous manual check confirmed
        // this passed before this test existed; committing the check rather than relying on
        // having run it once.
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var configuration = CreateConfiguration("Sqlite");
            var configWithConnection = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] = $"Data Source={dbPath}"
                })
                .Build();

            var services = new ServiceCollection();
            var environment = new StubHostEnvironment { ContentRootPath = Path.GetTempPath() };
            services.AddConfiguredDbContext<AppDbContext>(configWithConnection, environment);

            await using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.OpenConnectionAsync();

            await using var probe = new SqliteConnection($"Data Source={dbPath}");
            await probe.OpenAsync();
            await using var command = probe.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            var journalMode = (string)(await command.ExecuteScalarAsync())!;

            journalMode.ShouldBe("wal", StringCompareShould.IgnoreCase);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var f = dbPath + suffix;
                if (File.Exists(f)) File.Delete(f);
            }
        }
    }

    private static IConfiguration CreateConfiguration(string? provider)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sqlite"] = "Sqlite-connection",
            ["ConnectionStrings:Postgres"] = "Postgres-connection",
            ["ConnectionStrings:SqlServer"] = "SqlServer-connection"
        };

        if (provider is not null)
        {
            settings["Database:Provider"] = provider;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ForgeKit.Api.Tests";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
