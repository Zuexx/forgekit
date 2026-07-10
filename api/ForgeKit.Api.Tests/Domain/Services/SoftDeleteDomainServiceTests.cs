using ForgeKit.Api.Domain.Services;
using ForgeKit.Api.Entities.Todos;
using Xunit;

namespace ForgeKit.Api.Tests.Domain.Services;

public class SoftDeleteDomainServiceTests
{
    private static TodoItem CreateTestTodoItem() => new TodoItem
    {
        WorkspaceId = "ws-123",
        Title = "Test Todo",
        Priority = "Low",
        CurrentStatus = "Todo"
    };

    [Fact]
    public void MarkAsDeleted_WithValidEntity_SetsDeleteFields()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        var userId = "user-456";

        // Act
        service.MarkAsDeleted(entity, userId);

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.True((DateTime.UtcNow - entity.DeletedAt.Value).TotalSeconds < 5);
        Assert.Equal(userId, entity.DeletedBy);
    }

    [Fact]
    public void MarkAsDeleted_WithCustomDeletedAt_UsesProvidedTimestamp()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        var userId = "user-456";
        var customDateTime = DateTime.UtcNow.AddDays(-1);

        // Act
        service.MarkAsDeleted(entity, userId, customDateTime);

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.Equal(customDateTime, entity.DeletedAt);
        Assert.Equal(userId, entity.DeletedBy);
    }

    [Fact]
    public void MarkAsDeleted_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.MarkAsDeleted<TodoItem>(null!, "user-123"));
    }

    [Fact]
    public void MarkAsDeleted_WithNullDeletedBy_ThrowsArgumentException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.MarkAsDeleted(entity, null!));
    }

    [Fact]
    public void Restore_WithValidEntity_ClearsDeleteFields()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow.AddDays(-1);
        entity.DeletedBy = "user-deleted";
        var restoredBy = "user-restored";

        // Act
        service.Restore(entity, restoredBy);

        // Assert
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
        Assert.Equal(restoredBy, entity.UpdatedBy);
        Assert.True((DateTime.UtcNow - entity.UpdatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Restore_WithExpiredEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow.AddDays(-40);
        entity.DeletedBy = "user-deleted";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.Restore(entity, "user-restored"));
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal("user-deleted", entity.DeletedBy);
    }

    [Fact]
    public void Entity_CanBeDeletedRestoredAndDeletedAgain()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();

        // Act
        service.MarkAsDeleted(entity, "first-deleter", DateTime.UtcNow.AddDays(-1));
        service.Restore(entity, "restorer");
        service.MarkAsDeleted(entity, "second-deleter");

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal("second-deleter", entity.DeletedBy);
        Assert.Equal("restorer", entity.UpdatedBy);
    }

    [Fact]
    public void Restore_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Restore<TodoItem>(null!, "user-123"));
    }

    [Fact]
    public void Restore_WithNullRestoredBy_ThrowsArgumentException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Restore(entity, null!));
    }

    [Fact]
    public void CanRestore_WithRecentlyDeletedEntity_ReturnsTrue()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = service.CanRestore(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanRestore_WithOldDeletedEntity_ReturnsFalse()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow.AddDays(-40);

        // Act
        var result = service.CanRestore(entity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanRestore_WithActiveEntity_ReturnsFalse()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = false;

        // Act
        var result = service.CanRestore(entity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanRestore_WithCustomRestoreDaysLimit_RespectsLimit()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow.AddDays(-10);

        // Act
        var resultWith5Days = service.CanRestore(entity, 5);
        var resultWith15Days = service.CanRestore(entity, 15);

        // Assert
        Assert.False(resultWith5Days);
        Assert.True(resultWith15Days);
    }

    [Fact]
    public void CanRestore_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.CanRestore<TodoItem>(null!));
    }

    [Fact]
    public void MarkAsDeleted_WithEmptyStringDeletedBy_ThrowsArgumentException()
    {
        // Arrange
        var service = new SoftDeleteDomainService();
        var entity = CreateTestTodoItem();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.MarkAsDeleted(entity, ""));
    }
}
