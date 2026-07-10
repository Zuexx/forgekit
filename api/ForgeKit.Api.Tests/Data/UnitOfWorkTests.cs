using Shouldly;
using Microsoft.EntityFrameworkCore;
using ForgeKit.Api.Data;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;

namespace ForgeKit.Api.Tests.Data;

public class UnitOfWorkTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    private static Workspace CreateTestWorkspace(string id, string name = "Test Workspace")
    {
        return new Workspace
        {
            Id = id,
            WorkspaceCode = $"WS-{id[..8].ToUpper()}",
            WorkspaceName = name,
            IsDeleted = false
        };
    }

    private static TodoItem CreateTestTodoItem(string id, string workspaceId, string title = "Test Todo")
    {
        return new TodoItem
        {
            Id = id,
            WorkspaceId = workspaceId,
            Title = title,
            Priority = "Medium",
            CurrentStatus = "Todo",
            IsDeleted = false
        };
    }

    [Fact]
    public async Task CommitTransaction_WithMultipleAdds_SavesAll()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var workspaceId = Guid.NewGuid().ToString();
        var todoId = Guid.NewGuid().ToString();

        var workspace = CreateTestWorkspace(workspaceId, "Acme Corp");
        var todo = CreateTestTodoItem(todoId, workspaceId, "My First Todo");

        _unitOfWork.DbContext.Workspaces.Add(workspace);
        _unitOfWork.DbContext.TodoItems.Add(todo);

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        var savedTodo = await _dbContext.TodoItems.FirstOrDefaultAsync(x => x.Id == todoId);

        savedWorkspace.ShouldNotBeNull();
        savedTodo.ShouldNotBeNull();
        savedWorkspace!.WorkspaceName.ShouldBe("Acme Corp");
        savedTodo!.Title.ShouldBe("My First Todo");
    }

    [Fact]
    public async Task RollbackTransaction_WithAdd_DiscardsAll()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var workspaceId = Guid.NewGuid().ToString();
        var workspace = CreateTestWorkspace(workspaceId, "Jane Smith Workspace");

        _unitOfWork.DbContext.Workspaces.Add(workspace);

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        savedWorkspace.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithUserId_SetsCreatedBy()
    {
        // Arrange
        const string userId = "user123";
        var workspaceId = Guid.NewGuid().ToString();
        var workspace = CreateTestWorkspace(workspaceId, "Bob Wilson Workspace");

        _unitOfWork.DbContext.Workspaces.Add(workspace);

        // Act
        await _unitOfWork.SaveChangesAsync(userId);

        // Assert
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        savedWorkspace.ShouldNotBeNull();
        savedWorkspace!.CreatedBy.ShouldBe(userId);
        savedWorkspace.UpdatedBy.ShouldBe(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_SetsUpdatedBy()
    {
        // Arrange
        const string originalUser = "user1";
        const string updatingUser = "user2";
        var workspaceId = Guid.NewGuid().ToString();

        var workspace = CreateTestWorkspace(workspaceId, "Alice Brown Workspace");
        workspace.CreatedBy = originalUser;
        workspace.UpdatedBy = originalUser;

        _dbContext.Workspaces.Add(workspace);
        await _dbContext.SaveChangesAsync();

        // Act
        workspace.WorkspaceName = "Alicia Brown Workspace";
        await _unitOfWork.SaveChangesAsync(updatingUser);

        // Assert
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        savedWorkspace.ShouldNotBeNull();
        savedWorkspace!.CreatedBy.ShouldBe(originalUser);
        savedWorkspace.UpdatedBy.ShouldBe(updatingUser);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutTransaction_Commits()
    {
        // Arrange & Act
        var workspaceId = Guid.NewGuid().ToString();
        var workspace = CreateTestWorkspace(workspaceId, "Grace Foster Workspace");
        _unitOfWork.DbContext.Workspaces.Add(workspace);
        await _unitOfWork.SaveChangesAsync("system");

        // Assert - verify entity was saved directly (no transaction)
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        savedWorkspace.ShouldNotBeNull();
        savedWorkspace!.WorkspaceName.ShouldBe("Grace Foster Workspace");
    }

    [Fact]
    public void DbContext_Property_ReturnsDbContextInstance()
    {
        // Act
        var dbContext = _unitOfWork.DbContext;

        // Assert
        dbContext.ShouldNotBeNull();
        dbContext.ShouldBeSameAs(_dbContext);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutUserId_DoesNotSetAuditFields()
    {
        // Arrange
        var workspaceId = Guid.NewGuid().ToString();
        var workspace = CreateTestWorkspace(workspaceId, "David Evans Workspace");

        _unitOfWork.DbContext.Workspaces.Add(workspace);

        // Act
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedWorkspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        savedWorkspace.ShouldNotBeNull();
        savedWorkspace!.CreatedBy.ShouldBeNull();
        savedWorkspace.UpdatedBy.ShouldBeNull();
    }

    public void Dispose()
    {
        _unitOfWork?.Dispose();
        _dbContext?.Dispose();
    }
}
