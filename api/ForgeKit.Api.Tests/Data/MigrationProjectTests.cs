using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using PostgresMigrations = ForgeKit.Api.Migrations.Postgres;
using SqliteMigrations = ForgeKit.Api.Migrations.Sqlite;
using SqlServerMigrations = ForgeKit.Api.Migrations.SqlServer;

namespace ForgeKit.Api.Tests.Data;

public sealed class MigrationProjectTests
{
    [Fact]
    public void SqliteFactories_ShouldUseSqliteMigrationAssembly()
    {
        using var appContext = new SqliteMigrations.AppDbContextFactory().CreateDbContext([]);
        using var authContext = new SqliteMigrations.BetterAuthDbContextFactory().CreateDbContext([]);

        AssertMigrationConfiguration(
            appContext,
            "Microsoft.EntityFrameworkCore.Sqlite",
            "ForgeKit.Api.Migrations.Sqlite");
        AssertMigrationConfiguration(
            authContext,
            "Microsoft.EntityFrameworkCore.Sqlite",
            "ForgeKit.Api.Migrations.Sqlite");
    }

    [Fact]
    public void PostgresFactories_ShouldUsePostgresMigrationAssembly()
    {
        using var appContext = new PostgresMigrations.AppDbContextFactory().CreateDbContext([]);
        using var authContext = new PostgresMigrations.BetterAuthDbContextFactory().CreateDbContext([]);

        AssertMigrationConfiguration(
            appContext,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "ForgeKit.Api.Migrations.Postgres");
        AssertMigrationConfiguration(
            authContext,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "ForgeKit.Api.Migrations.Postgres");
    }

    [Fact]
    public void SqlServerFactories_ShouldUseSqlServerMigrationAssembly()
    {
        using var appContext = new SqlServerMigrations.AppDbContextFactory().CreateDbContext([]);
        using var authContext = new SqlServerMigrations.BetterAuthDbContextFactory().CreateDbContext([]);

        AssertMigrationConfiguration(
            appContext,
            "Microsoft.EntityFrameworkCore.SqlServer",
            "ForgeKit.Api.Migrations.SqlServer");
        AssertMigrationConfiguration(
            authContext,
            "Microsoft.EntityFrameworkCore.SqlServer",
            "ForgeKit.Api.Migrations.SqlServer");
    }

    private static void AssertMigrationConfiguration(
        DbContext context,
        string expectedProvider,
        string expectedAssembly)
    {
        context.Database.ProviderName.ShouldBe(expectedProvider);
        context.GetService<IMigrationsAssembly>().Assembly.GetName().Name.ShouldBe(expectedAssembly);
    }
}
