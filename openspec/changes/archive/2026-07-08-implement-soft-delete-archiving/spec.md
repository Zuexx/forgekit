# Soft-Delete Archiving Pattern Specification

## Summary

Soft-delete archiving enables logical deletion of entities with full restoration capability within a configurable grace period, maintaining complete audit trails while supporting compliance and recovery scenarios.

## Status

**Draft** - Referenced in additional-findings-recommendations.md as item #5

## What is Soft-Delete Archiving?

### Soft-Delete
```
Instead of physically removing records, mark as deleted:
- IsDeleted: true
- DeletedAt: timestamp
- DeletedBy: user ID
```

### Archiving
```
Allow restoration within grace period:
- Restore: IsDeleted=false, clear DeletedAt/DeletedBy
- CanRestore: Check business rules (e.g., 30-day window)
```

### Benefits
- **Recovery**: Users can undo accidental deletions
- **Compliance**: Maintains audit trail of all changes
- **Safety**: No data loss, enables business intelligence
- **Reversibility**: Actions are reversible, not permanent

## Current State

✅ **Implemented:**
- `BaseEntity.IsDeleted`, `DeletedAt`, `DeletedBy` fields
- Global query filters: `HasQueryFilter(e => !e.IsDeleted)`
- AppDbContext automatically filters out deleted records

❌ **Missing:**
- Restore capability
- Business rules for restore eligibility
- Centralized soft-delete service
- Restore operations in application layer

## Specification

### SoftDeleteDomainService

#### MarkAsDeleted<T>()

```csharp
public void MarkAsDeleted<T>(T entity, string deletedBy) 
    where T : BaseEntity
```

**Purpose**: Marks entity as deleted with audit trail

**Behavior**:
```csharp
entity.IsDeleted = true;
entity.DeletedAt = DateTime.UtcNow;
entity.DeletedBy = deletedBy;
```

**Preconditions**:
- Entity is not already deleted
- deletedBy is non-null and non-empty

**Postconditions**:
- Entity has IsDeleted=true
- DeletedAt and DeletedBy are set
- Entity remains in DbContext (not removed)

---

#### Restore<T>()

```csharp
public void Restore<T>(T entity, string restoredBy) 
    where T : BaseEntity
```

**Purpose**: Restores a deleted entity to active state

**Behavior**:
```csharp
if (!CanRestore(entity))
    throw new InvalidOperationException(...);

entity.IsDeleted = false;
entity.DeletedAt = null;
entity.DeletedBy = null;
entity.UpdatedBy = restoredBy;
entity.UpdatedAt = DateTime.UtcNow;
```

**Preconditions**:
- Entity is deleted (IsDeleted=true)
- Entity meets restore eligibility criteria (CanRestore=true)
- restoredBy is non-null and non-empty

**Postconditions**:
- Entity has IsDeleted=false
- DeletedAt and DeletedBy are cleared
- UpdatedBy and UpdatedAt are set to restore timestamp and user
- Entity is now visible to normal queries

**Exceptions**:
- `InvalidOperationException`: If CanRestore() returns false

---

#### CanRestore<T>()

```csharp
public bool CanRestore<T>(T entity) 
    where T : BaseEntity
```

**Purpose**: Determines if entity can be restored based on business rules

**Business Rules**:
1. Entity must be deleted: `entity.IsDeleted == true`
2. Must have DeletedAt timestamp: `entity.DeletedAt.HasValue`
3. Deletion must be within grace period: `(UtcNow - DeletedAt) < 30 days`

**Returns**:
- `true`: Entity can be restored
- `false`: Entity cannot be restored (grace period expired or not deleted)

**Rationale for 30-day grace period**:
- Typical compliance requirement
- Balances recovery with data cleanup
- Allows investigations and historical queries
- Aligns with GDPR right-to-be-forgotten timelines

---

## Query Filter Behavior

### Default Queries (With Filter)

```csharp
// Returns only non-deleted entities
var activeVisits = await dbContext.TodoItems.ToListAsync();
```

### Restore Operations (Ignore Filter)

```csharp
// Include deleted entities for restoration
var deletedVisit = await dbContext.TodoItems
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(v => v.Id == visitId && v.IsDeleted);
```

### Admin Operations (Optional)

```csharp
// View all records including deleted
var allRecords = await dbContext.TodoItems
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## Application Service Integration

### Deletion Flow

```csharp
public async Task DeleteTodoItemAsync(string todoItemId)
{
    // 1. Load entity (normal query excludes soft-deleted)
    var todoItem = await _unitOfWork.DbContext.TodoItems
        .FirstOrDefaultAsync(v => v.Id == todoItemId);
    
    if (todoItem == null)
        throw new NotFoundException("Visit request not found");

    // 2. Mark as deleted using domain service
    _softDeleteService.MarkAsDeleted(todoItem, _auditContext.UserId);
    
    // 3. Persist in transaction
    await _unitOfWork.SaveChangesAsync(_auditContext.UserId);
}
```

### Restoration Flow

```csharp
public async Task RestoreTodoItemAsync(string todoItemId)
{
    // 1. Load deleted entity (IgnoreQueryFilters needed)
    var todoItem = await _unitOfWork.DbContext.TodoItems
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(v => v.Id == todoItemId && v.IsDeleted);
    
    if (todoItem == null)
        throw new NotFoundException("Deleted visit request not found");

    // 2. Check if restoration is allowed
    if (!_softDeleteService.CanRestore(todoItem))
        throw new InvalidOperationException(
            "Cannot restore visit request. Deletion is final after 30 days.");

    // 3. Restore using domain service
    _softDeleteService.Restore(todoItem, _auditContext.UserId);
    
    // 4. Persist in transaction
    await _unitOfWork.SaveChangesAsync(_auditContext.UserId);
}
```

---

## Audit Trail Example

### Scenario: Create, Update, Delete, Restore

| Operation | Timestamp | User | IsDeleted | DeletedAt | DeletedBy | UpdatedBy | UpdatedAt |
|-----------|-----------|------|-----------|-----------|-----------|-----------|-----------|
| Create | 10:00:00 | john | false | - | - | john | 10:00:00 |
| Update | 10:15:00 | john | false | - | - | john | 10:15:00 |
| Delete | 10:30:00 | admin | **true** | 10:30:00 | **admin** | admin | 10:30:00 |
| Restore | 10:45:00 | admin | **false** | **null** | **null** | admin | 10:45:00 |

**Key Insight**: Original CreatedBy/CreatedAt unchanged through entire lifecycle

---

## Testing Strategy

### Unit Tests (SoftDeleteDomainService)

```csharp
[Fact]
public void MarkAsDeleted_SetsDeleteFlags()
{
    var entity = new TodoItem { /* ... */ };
    _service.MarkAsDeleted(entity, "user123");
    
    Assert.True(entity.IsDeleted);
    Assert.NotNull(entity.DeletedAt);
    Assert.Equal("user123", entity.DeletedBy);
}

[Fact]
public void CanRestore_ReturnsTrueWithin30Days()
{
    var entity = new TodoItem 
    { 
        IsDeleted = true,
        DeletedAt = DateTime.UtcNow.AddDays(-15)
    };
    
    Assert.True(_service.CanRestore(entity));
}

[Fact]
public void CanRestore_ReturnsFalseAfter30Days()
{
    var entity = new TodoItem 
    { 
        IsDeleted = true,
        DeletedAt = DateTime.UtcNow.AddDays(-31)
    };
    
    Assert.False(_service.CanRestore(entity));
}
```

### Integration Tests

```csharp
[Fact]
public async Task Delete_ExcludesFromQueries()
{
    var todoItem = await CreateTodoItemAsync();
    _softDeleteService.MarkAsDeleted(todoItem, "user1");
    await _unitOfWork.SaveChangesAsync();
    
    var found = await _dbContext.TodoItems
        .FirstOrDefaultAsync(v => v.Id == todoItem.Id);
    
    Assert.Null(found);  // Not found in normal query
}

[Fact]
public async Task Restore_IncludesInQueries()
{
    var todoItem = await CreateAndDeleteTodoItemAsync();
    _softDeleteService.Restore(todoItem, "user2");
    await _unitOfWork.SaveChangesAsync();
    
    var found = await _dbContext.TodoItems
        .FirstOrDefaultAsync(v => v.Id == todoItem.Id);
    
    Assert.NotNull(found);
    Assert.False(found.IsDeleted);
}
```

---

## Configuration

### Grace Period

**Constant**: `SoftDeleteDomainService.MaxRestoreDays = 30`

**To Change**:
```csharp
// In SoftDeleteDomainService
private const int MaxRestoreDays = 90; // Change to 90 days
```

### Enable Soft-Delete for New Entities

1. Inherit from `BaseEntity` (or implement `ISoftDeletable`)
2. Add query filter in `AppDbContext.OnModelCreating()`:
   ```csharp
   modelBuilder.Entity<YourEntity>()
       .HasQueryFilter(e => !e.IsDeleted);
   ```

---

## Related Specifications

- **Audit Context** (audit-context.md): Provides UserId/UserName
- **Unit of Work** (unit-of-work.md): Manages transactions
- **Domain Services**: Use SoftDeleteDomainService for consistency

---

## Acceptance Criteria

- ✅ SoftDeleteDomainService provides MarkAsDeleted, Restore, CanRestore
- ✅ Restore enforces 30-day grace period
- ✅ Global query filters exclude soft-deleted records
- ✅ IgnoreQueryFilters() retrieves deleted records for restore
- ✅ Audit fields correctly updated on delete and restore
- ✅ All tests pass (unit + integration)
- ✅ No breaking changes to existing soft-delete behavior
