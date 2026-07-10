using ForgeKit.Api.Data;
using ForgeKit.Api.Domain.Services;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Services.Todos;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ForgeKit.Api.Tests.Services;

public sealed class TodoServiceSoftDeleteTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;
    private readonly TestAuditContext _auditContext = new("sample-user");
    private readonly TodoService _todoService;

    public TodoServiceSoftDeleteTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
        _todoService = new TodoService(_unitOfWork, _auditContext, new SoftDeleteDomainService());
    }

    [Fact]
    public async Task DeleteTodoAsync_ShouldUseSoftDeleteDomainService()
    {
        // Arrange
        var todo = await SeedTodoAsync();

        // Act
        var deleted = await _todoService.DeleteTodoAsync(todo.Id);

        // Assert
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedBy.ShouldBe(_auditContext.UserId);
        deleted.DeletedAt.ShouldNotBeNull();
        deleted.UpdatedBy.ShouldBe(_auditContext.UserId);
    }

    [Fact]
    public async Task SoftDeletedTodo_ShouldBeExcludedByDefaultQueryFilter()
    {
        // Arrange
        var todo = await SeedTodoAsync();
        await _todoService.DeleteTodoAsync(todo.Id);

        // Act
        var defaultQueryResult = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == todo.Id);
        var ignoredFilterResult = await _dbContext.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == todo.Id);

        // Assert
        defaultQueryResult.ShouldBeNull();
        ignoredFilterResult.ShouldNotBeNull();
        ignoredFilterResult!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task RestoreTodoAsync_ShouldUseIgnoreQueryFiltersAndRestoreEligibleTodo()
    {
        // Arrange
        var todo = await SeedTodoAsync(createdBy: "creator");
        todo.CreatedBy = "creator";
        todo.UpdatedBy = "modifier";
        await _dbContext.SaveChangesAsync();
        await _todoService.DeleteTodoAsync(todo.Id);

        // Act
        var restored = await _todoService.RestoreTodoAsync(todo.Id);

        // Assert
        restored.IsDeleted.ShouldBeFalse();
        restored.DeletedAt.ShouldBeNull();
        restored.DeletedBy.ShouldBeNull();
        restored.CreatedBy.ShouldBe("creator");
        restored.UpdatedBy.ShouldBe(_auditContext.UserId);

        var defaultQueryResult = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == todo.Id);
        defaultQueryResult.ShouldNotBeNull();
    }

    [Fact]
    public async Task RestoreTodoAsync_ShouldRejectTodoBeyondGracePeriod()
    {
        // Arrange
        var todo = await SeedTodoAsync();
        todo.IsDeleted = true;
        todo.DeletedAt = DateTime.UtcNow.AddDays(-40);
        todo.DeletedBy = "deleter";
        await _dbContext.SaveChangesAsync();

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _todoService.RestoreTodoAsync(todo.Id));

        // Assert
        exception.Message.ShouldContain("restore grace period");
        todo.IsDeleted.ShouldBeTrue();
        todo.DeletedBy.ShouldBe("deleter");
    }

    private async Task<TodoItem> SeedTodoAsync(string createdBy = "creator")
    {
        var workspace = new Workspace
        {
            WorkspaceCode = $"WS-{Guid.NewGuid():N}"[..20],
            WorkspaceName = "Sample Workspace"
        };

        var todo = new TodoItem
        {
            WorkspaceId = workspace.Id,
            Title = "Sample Todo",
            Priority = "Medium",
            CurrentStatus = "Todo",
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        _dbContext.Workspaces.Add(workspace);
        _dbContext.TodoItems.Add(todo);
        await _dbContext.SaveChangesAsync();

        return todo;
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _dbContext.Dispose();
    }

    private sealed class TestAuditContext(string userId) : IAuditContext
    {
        public string UserId { get; } = userId;
        public string UserName => UserId;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
