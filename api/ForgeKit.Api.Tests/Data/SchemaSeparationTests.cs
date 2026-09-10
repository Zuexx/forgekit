using Microsoft.EntityFrameworkCore;
using Shouldly;
using PostgresMigrations = ForgeKit.Api.Migrations.Postgres;
using SqlServerMigrations = ForgeKit.Api.Migrations.SqlServer;
using SqliteMigrations = ForgeKit.Api.Migrations.Sqlite;

namespace ForgeKit.Api.Tests.Data;

public sealed class SchemaSeparationTests
{
    [Fact]
    public void Postgres_BetterAuthDbContext_UsesAuthSchema()
    {
        using var context = new PostgresMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldBe("auth");
    }

    [Fact]
    public void Postgres_AppDbContext_DoesNotUseAuthSchema()
    {
        using var context = new PostgresMigrations.AppDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldNotBe("auth");
    }

    [Fact]
    public void SqlServer_BetterAuthDbContext_UsesAuthSchema()
    {
        using var context = new SqlServerMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldBe("auth");
    }

    [Fact]
    public void SqlServer_AppDbContext_DoesNotUseAuthSchema()
    {
        using var context = new SqlServerMigrations.AppDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldNotBe("auth");
    }

    [Fact]
    public void Sqlite_BetterAuthDbContext_GeneratesNoSchemaQualifiedSql()
    {
        // The provider accepts HasDefaultSchema but SQLite has no schema concept, so the
        // generated CREATE TABLE statement must not be schema-qualified. This is the
        // behavioural check; the model-level annotation may still be present (that part is
        // not asserted here — it is a provider detail, not a contract this kit depends on).
        using var context = new SqliteMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        var sql = context.Database.GenerateCreateScript();
        sql.ShouldNotContain("\"auth\".");
    }
}
