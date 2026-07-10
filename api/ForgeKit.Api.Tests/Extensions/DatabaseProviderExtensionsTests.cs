using ForgeKit.Api.Extensions;
using Microsoft.Extensions.Configuration;
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
}
