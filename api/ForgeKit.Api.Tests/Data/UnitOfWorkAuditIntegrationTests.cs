using ForgeKit.Api.Data;
using ForgeKit.Api.Entities.Todos;
using ForgeKit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ForgeKit.Api.Tests.Data;

/// <summary>
/// UnitOfWork Audit Integration Tests
/// 
/// TRANSACTION TESTING LIMITATION:
/// ==============================
/// The in-memory database provider does NOT support real transactions.
/// Methods like BeginTransactionAsync(), CommitTransactionAsync(), and 
/// RollbackTransactionAsync() are no-ops in the in-memory provider.
/// 
/// These tests exercise the transaction APIs but DO NOT provide real validation
/// of transaction atomicity or rollback behavior.
/// </summary>
public class UnitOfWorkAuditIntegrationTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("test_" + Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static TodoItem CreateTestTodoItem() => new TodoItem
    {
        WorkspaceId = "ws-123",
        Title = "Test Todo",
        Priority = "Low",
        CurrentStatus = "Requested"
    };

    [Fact]
    public async Task SaveChangesAsync_WithUserId_SetsCreatedByAndUpdatedByOnNewEntity()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var userId = "test-user-123";
        var entity = CreateTestTodoItem();

        // Act
        dbContext.TodoItems.Add(entity);
        await unitOfWork.SaveChangesAsync(userId);

        // Assert
        Assert.Equal(userId, entity.CreatedBy);
        Assert.Equal(userId, entity.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WithUserId_SetsUpdatedByOnModifiedEntity()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var originalUserId = "original-user";
        var modifyingUserId = "modifying-user";

        var entity = CreateTestTodoItem();
        entity.CreatedBy = originalUserId;
        entity.UpdatedBy = originalUserId;

        dbContext.TodoItems.Add(entity);
        await dbContext.SaveChangesAsync();

        // Act - Modify and save with different user
        entity.CurrentStatus = "InProgress";
        await unitOfWork.SaveChangesAsync(modifyingUserId);

        // Assert
        Assert.Equal(originalUserId, entity.CreatedBy);
        Assert.Equal(modifyingUserId, entity.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutUserId_DoesNotSetAuditFields()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var entity = CreateTestTodoItem();

        // Act
        dbContext.TodoItems.Add(entity);
        await unitOfWork.SaveChangesAsync(); // No userId parameter

        // Assert
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact(Skip = "In-memory database does not support real transactions. For transaction validation, use SQL Server tempdb.")]
    public async Task Transaction_CommitAtomically_SavesAllEntitiesOrNone()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var userId = "test-user";

        var entity1 = CreateTestTodoItem();
        var entity2 = new TodoItem
        {
            WorkspaceId = "ws-456",
            Title = "Second Todo",
            Priority = "High",
            CurrentStatus = "Todo"
        };

        // Act
        await unitOfWork.BeginTransactionAsync();
        dbContext.TodoItems.Add(entity1);
        dbContext.TodoItems.Add(entity2);
        await unitOfWork.SaveChangesAsync(userId);
        await unitOfWork.CommitTransactionAsync();

        // Assert
        var savedEntities = await dbContext.TodoItems.ToListAsync();
        Assert.Equal(2, savedEntities.Count);
        Assert.All(savedEntities, e =>
        {
            Assert.Equal(userId, e.CreatedBy);
            Assert.Equal(userId, e.UpdatedBy);
        });
    }

    [Fact(Skip = "In-memory database does not support real transactions. For transaction validation, use SQL Server tempdb.")]
    public async Task Transaction_RollbackOnError_Reverts_AllChanges()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var userId = "test-user";
        var entity = CreateTestTodoItem();

        // Act
        try
        {
            await unitOfWork.BeginTransactionAsync();
            dbContext.TodoItems.Add(entity);
            await unitOfWork.SaveChangesAsync(userId);
            // Simulate error
            throw new Exception("Simulated error");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
        }

        // Assert
        var savedEntities = await dbContext.TodoItems.ToListAsync();
        Assert.Empty(savedEntities);
    }

    [Fact(Skip = "In-memory database does not support real transactions. For transaction validation, use SQL Server tempdb.")]
    public async Task MultipleEntities_InTransaction_AllGetSameAuditUser()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(dbContext);
        var userId = "single-user";

        var entities = new[]
        {
            new TodoItem { WorkspaceId = "ws-1", Title = "Todo 1", Priority = "Low", CurrentStatus = "Todo" },
            new TodoItem { WorkspaceId = "ws-2", Title = "Todo 2", Priority = "Medium", CurrentStatus = "Todo" },
            new TodoItem { WorkspaceId = "ws-3", Title = "Todo 3", Priority = "High", CurrentStatus = "Todo" }
        };

        // Act
        await unitOfWork.BeginTransactionAsync();
        foreach (var entity in entities)
        {
            dbContext.TodoItems.Add(entity);
        }
        await unitOfWork.SaveChangesAsync(userId);
        await unitOfWork.CommitTransactionAsync();

        // Assert
        var savedEntities = await dbContext.TodoItems.ToListAsync();
        Assert.Equal(3, savedEntities.Count);
        Assert.All(savedEntities, e =>
        {
            Assert.Equal(userId, e.CreatedBy);
            Assert.Equal(userId, e.UpdatedBy);
        });
    }
}
