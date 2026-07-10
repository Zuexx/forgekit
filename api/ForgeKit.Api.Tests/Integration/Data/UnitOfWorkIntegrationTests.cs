using ForgeKit.Api.Data;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using ForgeKit.Api.Interfaces;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ForgeKit.Api.Tests.Integration.Data;

public class UnitOfWorkIntegrationTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private IServiceScope? _scope;
    private AppDbContext? _dbContext;
    private IUnitOfWork? _unitOfWork;

    public UnitOfWorkIntegrationTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await _dbContext!.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WithUserId_SetsAuditFields()
    {
        // Arrange
        var todoItem = new TodoItem
        {
            WorkspaceId = "ws-audit-test",
            Workspace = CreateWorkspace("ws-audit-test"),
            Title = "Audit Test Todo",
            Priority = "Low",
            CurrentStatus = "Todo"
        };

        // Act
        _dbContext!.TodoItems.Add(todoItem);
        await _unitOfWork!.SaveChangesAsync("audit-user");

        // Assert
        todoItem.CreatedBy.ShouldBe("audit-user");
        todoItem.UpdatedBy.ShouldBe("audit-user");
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutUserId_DoesNotSetAuditFields()
    {
        // Arrange
        var todoItem = new TodoItem
        {
            WorkspaceId = "ws-no-audit",
            Workspace = CreateWorkspace("ws-no-audit"),
            Title = "No Audit Todo",
            Priority = "Low",
            CurrentStatus = "Todo"
        };

        // Act
        _dbContext!.TodoItems.Add(todoItem);
        await _unitOfWork!.SaveChangesAsync();

        // Assert
        todoItem.CreatedBy.ShouldBeNull();
        todoItem.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_UpdatedEntity_SetsUpdatedByOnly()
    {
        // Arrange
        var originalUser = "creator";
        var updater = "updater";

        var todoItem = new TodoItem
        {
            WorkspaceId = "ws-update",
            Workspace = CreateWorkspace("ws-update"),
            Title = "Update Test Todo",
            Priority = "Low",
            CurrentStatus = "Todo",
            CreatedBy = originalUser,
            UpdatedBy = originalUser
        };

        _dbContext!.TodoItems.Add(todoItem);
        await _dbContext.SaveChangesAsync();

        // Act
        todoItem.CurrentStatus = "InProgress";
        await _unitOfWork!.SaveChangesAsync(updater);

        // Assert
        todoItem.CreatedBy.ShouldBe(originalUser);
        todoItem.UpdatedBy.ShouldBe(updater);
    }

    private static Workspace CreateWorkspace(string code)
    {
        return new Workspace
        {
            Id = code,
            WorkspaceCode = code,
            WorkspaceName = code
        };
    }
}
