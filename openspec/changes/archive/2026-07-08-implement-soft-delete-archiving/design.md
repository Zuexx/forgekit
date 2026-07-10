# Design: Soft-Delete Archiving Pattern

## Overview

The soft-delete archiving pattern manages entity lifecycle through logical deletion rather than physical removal, with support for restoration within a configurable grace period.

## Architecture

### Domain Service Layer

```csharp
// Api/Domain/Services/SoftDeleteDomainService.cs
public class SoftDeleteDomainService
{
    /// <summary>
    /// Marks an entity as deleted with audit trail.
    /// </summary>
    public void MarkAsDeleted<T>(T entity, string deletedBy) 
        where T : BaseEntity
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a deleted entity if eligible under business rules.
    /// </summary>
    public void Restore<T>(T entity, string restoredBy) 
        where T : BaseEntity
    {
        if (!CanRestore(entity))
            throw new InvalidOperationException(
                $"Entity cannot be restored. Deletion is final after {MaxRestoreDays} days.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.UpdatedBy = restoredBy;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if an entity can be restored based on business rules.
    /// </summary>
    public bool CanRestore<T>(T entity) 
        where T : BaseEntity
    {
        // Rule: Only restore if deleted less than 30 days ago
        return entity.IsDeleted && 
               entity.DeletedAt.HasValue &&
               (DateTime.UtcNow - entity.DeletedAt.Value).TotalDays < MaxRestoreDays;
    }

    private const int MaxRestoreDays = 30;
}
```

## Entities

### BaseEntity (Existing)

```csharp
public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Audit Fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-delete fields (enhanced with archiving)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Query Filters

All entities with soft-delete use global query filters to exclude deleted records:

```csharp
// In AppDbContext.OnModelCreating()
modelBuilder.Entity<TodoItem>()
    .HasQueryFilter(e => !e.IsDeleted);

modelBuilder.Entity<TodoStatusHistory>()
    .HasQueryFilter(e => !e.IsDeleted);

// ... for all deletable entities
```

## Integration Points

### Application Service Usage

```csharp
public class TodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditContext _auditContext;
    private readonly SoftDeleteDomainService _softDeleteService;

    public async Task DeleteTodoItemAsync(string todoItemId)
    {
        var todoItem = await _unitOfWork.DbContext.TodoItems
            .FirstOrDefaultAsync(v => v.Id == todoItemId);
        
        if (todoItem == null)
            throw new NotFoundException("Visit request not found");

        // Use domain service for consistent deletion
        _softDeleteService.MarkAsDeleted(todoItem, _auditContext.UserId);
        
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId);
    }

    public async Task RestoreTodoItemAsync(string todoItemId)
    {
        // Query includes deleted records to restore
        var todoItem = await _unitOfWork.DbContext.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == todoItemId);
        
        if (todoItem == null)
            throw new NotFoundException("Visit request not found");

        if (!_softDeleteService.CanRestore(todoItem))
            throw new InvalidOperationException(
                "Visit request cannot be restored (beyond grace period)");

        _softDeleteService.Restore(todoItem, _auditContext.UserId);
        
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId);
    }
}
```

## DI Registration

```csharp
// Program.cs
builder.Services.AddScoped<SoftDeleteDomainService>();
```

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic `MarkAsDeleted<T>()` | Works with any BaseEntity subclass |
| 30-day grace period | Typical for compliance/audit requirements |
| `CanRestore()` method | Encapsulates business rules for testability |
| Null out DeletedBy on restore | Clear audit trail (original deletion is still visible in history/events) |
| Use `IgnoreQueryFilters()` for restore | Only needed when explicitly accessing deleted entities |
| No physical deletion | Maintains full audit trail forever |

## Audit Trail Behavior

### Deletion
```
Original: IsDeleted=false, DeletedAt=null, DeletedBy=null, UpdatedBy="user1"
After Delete: IsDeleted=true, DeletedAt="2026-02-09T12:00:00Z", DeletedBy="user2", UpdatedBy="user2"
```

### Restoration (within 30 days)
```
Before Restore: IsDeleted=true, DeletedAt="2026-02-09T12:00:00Z", DeletedBy="user2", UpdatedBy="user2"
After Restore: IsDeleted=false, DeletedAt=null, DeletedBy=null, UpdatedBy="user3"
```

### Restoration Attempt (after 30 days)
```
Before Restore: IsDeleted=true, DeletedAt="2026-01-01T12:00:00Z", DeletedBy="user2"
Result: ❌ InvalidOperationException thrown
```

## Testing Strategy

- Unit tests for MarkAsDeleted(), Restore(), CanRestore()
- Time-based eligibility tests (mocked DateTime)
- Integration tests with UnitOfWork
- Query filter tests (soft-deleted records excluded by default)
- IgnoreQueryFilters() tests (for restore operations)

## Future Enhancements

- Event sourcing to preserve full deletion history
- Configurable grace period per entity type
- Batch restoration operations
- Retention policies for automatic physical deletion
- Archive database for long-term compliance
