using Anvil.Data;
using Anvil.Entities.Base;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Anvil.Tests.Data;

/// <summary>
/// Tests the shared layer's model conventions directly, using entities defined here
/// rather than product entities.
/// </summary>
/// <remarks>
/// Soft-delete filters are derived by reflection over <see cref="ISoftDelete"/> rather
/// than listed per entity, so the property worth testing is not "Workspace is filtered"
/// but "every implementer is filtered and nothing else is". A behavioral test against a
/// single product entity passes even if reflection silently stops covering the rest.
/// </remarks>
public sealed class PlatformDbContextTests
{
    private sealed class Filtered : ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class Unfiltered
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : PlatformDbContext(options)
    {
        public DbSet<Filtered> Filtered { get; set; } = null!;
        public DbSet<Unfiltered> Unfiltered { get; set; } = null!;
    }

    private static TestContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void SoftDeleteFilter_AppliesToImplementers()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Filtered));

        entity.ShouldNotBeNull();
        entity.GetQueryFilter().ShouldNotBeNull();
    }

    [Fact]
    public void SoftDeleteFilter_SkipsNonImplementers()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Unfiltered));

        entity.ShouldNotBeNull();
        entity.GetQueryFilter().ShouldBeNull();
    }

    [Fact]
    public async Task SoftDeleteFilter_ExcludesDeletedRows()
    {
        await using var context = CreateContext();
        context.Filtered.AddRange(
            new Filtered { Name = "visible" },
            new Filtered { Name = "gone", IsDeleted = true, DeletedAt = DateTime.UtcNow, DeletedBy = "test" });
        await context.SaveChangesAsync();

        var visible = await context.Filtered.ToListAsync();
        var all = await context.Filtered.IgnoreQueryFilters().ToListAsync();

        visible.Select(f => f.Name).ShouldBe(["visible"]);
        all.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SoftDeleteFilter_LeavesNonImplementersUntouched()
    {
        await using var context = CreateContext();
        context.Unfiltered.AddRange(new Unfiltered { Name = "a" }, new Unfiltered { Name = "b" });
        await context.SaveChangesAsync();

        (await context.Unfiltered.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public void CamelCaseConvention_RenamesTablesAndColumns()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Filtered))!;

        entity.GetTableName().ShouldBe("filtered");
        entity.GetProperty(nameof(Filtered.IsDeleted)).GetColumnName().ShouldBe("isDeleted");
    }
}
