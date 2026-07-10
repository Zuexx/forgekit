using ForgeKit.Api.Data;
using ForgeKit.Api.Domain.Services;
using ForgeKit.Api.Entities.Todos;
using ForgeKit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Services.Todos;

/// <summary>
/// Sample application service demonstrating starter-kit conventions.
/// </summary>
/// <remarks>
/// This service intentionally stays small. It shows direct EF Core access through
/// Unit of Work, audit context usage, Result-friendly exceptions, and soft-delete
/// restoration with query filters.
/// </remarks>
public class TodoService(
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    SoftDeleteDomainService softDeleteService)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditContext _auditContext = auditContext;
    private readonly SoftDeleteDomainService _softDeleteService = softDeleteService;

    /// <summary>
    /// Creates a todo and initial status-history entry.
    /// </summary>
    public async Task<TodoItem> CreateTodoAsync(
        string workspaceId,
        string title,
        string priority = "Medium",
        string status = "Todo",
        DateTime? dueDate = null,
        string? assignedToMemberId = null,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            throw new ArgumentException("WorkspaceId cannot be empty", nameof(workspaceId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        var todo = new TodoItem
        {
            WorkspaceId = workspaceId,
            AssignedToMemberId = assignedToMemberId,
            CategoryId = categoryId,
            Title = title,
            Priority = priority,
            CurrentStatus = status,
            DueDate = dueDate
        };

        todo.StatusHistory.Add(new TodoStatusHistory
        {
            TodoItemId = todo.Id,
            Status = status,
            Timestamp = _auditContext.UtcNow,
            ChangedBy = _auditContext.UserId,
            Notes = "Created"
        });

        _unitOfWork.DbContext.TodoItems.Add(todo);
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, cancellationToken);

        return todo;
    }

    /// <summary>
    /// Updates todo status and records a status-history entry.
    /// </summary>
    public async Task<TodoItem> UpdateTodoStatusAsync(
        string todoId,
        string status,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var todo = await _unitOfWork.DbContext.TodoItems
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        if (todo is null)
            throw new KeyNotFoundException($"Todo '{todoId}' was not found.");

        todo.CurrentStatus = status;
        todo.CompletedAt = status.Equals("Done", StringComparison.OrdinalIgnoreCase)
            ? _auditContext.UtcNow
            : null;

        todo.StatusHistory.Add(new TodoStatusHistory
        {
            TodoItemId = todo.Id,
            Status = status,
            Timestamp = _auditContext.UtcNow,
            ChangedBy = _auditContext.UserId,
            Notes = notes
        });

        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, cancellationToken);

        return todo;
    }

    /// <summary>
    /// Soft-deletes a todo using the shared soft-delete domain service.
    /// </summary>
    public async Task<TodoItem> DeleteTodoAsync(string todoId, CancellationToken cancellationToken = default)
    {
        var todo = await _unitOfWork.DbContext.TodoItems
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        if (todo is null)
            throw new KeyNotFoundException($"Todo '{todoId}' was not found.");

        _softDeleteService.MarkAsDeleted(todo, _auditContext.UserId, _auditContext.UtcNow);
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, cancellationToken);

        return todo;
    }

    /// <summary>
    /// Restores a soft-deleted todo if it is still inside the restore grace period.
    /// </summary>
    public async Task<TodoItem> RestoreTodoAsync(string todoId, CancellationToken cancellationToken = default)
    {
        var todo = await _unitOfWork.DbContext.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        if (todo is null)
            throw new KeyNotFoundException($"Todo '{todoId}' was not found.");

        _softDeleteService.Restore(todo, _auditContext.UserId);
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, cancellationToken);

        return todo;
    }
}
