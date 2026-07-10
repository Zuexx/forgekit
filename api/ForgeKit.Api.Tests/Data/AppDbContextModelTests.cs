using ForgeKit.Api.Data;
using ForgeKit.Api.Entities.Analytics;
using ForgeKit.Api.Entities.Configuration;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace ForgeKit.Api.Tests.Data;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_ShouldConfigureCoreRelationships()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertForeignKey<Member, Workspace>(
            model,
            nameof(Member.WorkspaceId),
            nameof(Workspace.Members),
            DeleteBehavior.Restrict);

        AssertForeignKey<Category, Category>(
            model,
            nameof(Category.ParentCategoryId),
            nameof(Category.ChildCategories),
            DeleteBehavior.Restrict);

        AssertForeignKey<CategoryLabel, Category>(
            model,
            nameof(CategoryLabel.CategoryId),
            nameof(Category.CategoryLabels),
            DeleteBehavior.Restrict);

        AssertForeignKey<CategoryLabel, Label>(
            model,
            nameof(CategoryLabel.LabelId),
            nameof(Label.CategoryLabels),
            DeleteBehavior.Restrict);

        AssertForeignKey<WorkspaceAnalytics, Workspace>(
            model,
            nameof(WorkspaceAnalytics.WorkspaceId),
            nameof(Workspace.Analytics),
            DeleteBehavior.Restrict);

        AssertForeignKey<DailyActivitySnapshot, Workspace>(
            model,
            nameof(DailyActivitySnapshot.WorkspaceId),
            inverseNavigationName: null,
            DeleteBehavior.Restrict);

        AssertForeignKey<TodoItem, Workspace>(
            model,
            nameof(TodoItem.WorkspaceId),
            nameof(Workspace.TodoItems),
            DeleteBehavior.Restrict);

        AssertForeignKey<TodoItem, Member>(
            model,
            nameof(TodoItem.AssignedToMemberId),
            nameof(Member.AssignedTodoItems),
            DeleteBehavior.Restrict);

        AssertForeignKey<TodoItem, Category>(
            model,
            nameof(TodoItem.CategoryId),
            nameof(Category.TodoItems),
            DeleteBehavior.Restrict);

        AssertForeignKey<TodoStatusHistory, TodoItem>(
            model,
            nameof(TodoStatusHistory.TodoItemId),
            nameof(TodoItem.StatusHistory),
            DeleteBehavior.Cascade);
    }

    [Fact]
    public async Task Queries_ShouldExcludeSoftDeletedForgeKitEntities()
    {
        await using var context = CreateContext();
        var activeWorkspace = NewWorkspace("active", "Active");
        var deletedWorkspace = NewWorkspace("deleted", "Deleted");
        deletedWorkspace.IsDeleted = true;
        deletedWorkspace.DeletedAt = DateTime.UtcNow;
        deletedWorkspace.DeletedBy = "test-user";

        context.Workspaces.AddRange(activeWorkspace, deletedWorkspace);
        await context.SaveChangesAsync();

        var visibleCount = await context.Workspaces.CountAsync();
        var totalCount = await context.Workspaces.IgnoreQueryFilters().CountAsync();

        visibleCount.ShouldBe(1);
        totalCount.ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_ShouldUpdateAuditTimestampsAndVersion()
    {
        await using var context = CreateContext();
        var workspace = NewWorkspace("audit", "Audit");

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        workspace.CreatedAt.ShouldNotBe(default);
        workspace.UpdatedAt.ShouldNotBe(default);
        workspace.UpdatedAt.ShouldBe(workspace.CreatedAt);
        workspace.Version.ShouldBe(0);

        var firstUpdatedAt = workspace.UpdatedAt;
        workspace.WorkspaceName = "Audit Updated";

        await context.SaveChangesAsync();

        workspace.CreatedAt.ShouldNotBe(default);
        workspace.UpdatedAt.ShouldBeGreaterThanOrEqualTo(firstUpdatedAt);
        workspace.Version.ShouldBe(1);
    }

    [Fact]
    public void Model_ShouldConfigureAnalyticsIndexesAndMappings()
    {
        using var context = CreateContext();

        var workspaceAnalytics = context.Model.FindEntityType(typeof(WorkspaceAnalytics));
        workspaceAnalytics.ShouldNotBeNull();
        workspaceAnalytics.FindProperty(nameof(WorkspaceAnalytics.MetricsJson))!
            .GetColumnType()
            .ShouldBe("text");
        workspaceAnalytics.FindProperty(nameof(WorkspaceAnalytics.AverageCompletionDays))!
            .GetPrecision()
            .ShouldBe(18);
        workspaceAnalytics.FindProperty(nameof(WorkspaceAnalytics.AverageCompletionDays))!
            .GetScale()
            .ShouldBe(2);

        var workspaceIndex = FindIndex(
            workspaceAnalytics,
            nameof(WorkspaceAnalytics.PeriodStart),
            nameof(WorkspaceAnalytics.WorkspaceId),
            nameof(WorkspaceAnalytics.IsDeleted));
        workspaceIndex.ShouldNotBeNull();

        var dailySnapshot = context.Model.FindEntityType(typeof(DailyActivitySnapshot));
        dailySnapshot.ShouldNotBeNull();
        var dailyIndex = FindIndex(
            dailySnapshot,
            nameof(DailyActivitySnapshot.SnapshotDate),
            nameof(DailyActivitySnapshot.WorkspaceId),
            nameof(DailyActivitySnapshot.IsDeleted));
        dailyIndex.ShouldNotBeNull();
    }

    private static AppDbContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Workspace NewWorkspace(string code, string name)
    {
        return new Workspace
        {
            WorkspaceCode = code,
            WorkspaceName = name
        };
    }

    private static void AssertForeignKey<TEntity, TPrincipal>(
        IModel model,
        string foreignKeyPropertyName,
        string? inverseNavigationName,
        DeleteBehavior deleteBehavior)
    {
        var entityType = model.FindEntityType(typeof(TEntity));
        entityType.ShouldNotBeNull();

        var foreignKey = entityType.GetForeignKeys().SingleOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TPrincipal) &&
            fk.Properties.Select(p => p.Name).SequenceEqual([foreignKeyPropertyName]));

        foreignKey.ShouldNotBeNull();
        foreignKey.DeleteBehavior.ShouldBe(deleteBehavior);
        foreignKey.PrincipalToDependent?.Name.ShouldBe(inverseNavigationName);
    }

    private static IIndex? FindIndex(IEntityType entityType, params string[] propertyNames)
    {
        return entityType.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }
}
