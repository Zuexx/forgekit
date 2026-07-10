# OpenSpec Proposal: Implement Unit of Work Pattern

**Status:** Proposal  
**Created:** 2026-02-04  
**Priority:** High  
**Effort:** 2-3 days  
**Author:** Architecture Team  

---

## Overview

Implement a Unit of Work pattern to manage atomic transactions across multiple entity saves. This ensures data consistency when multiple entities need to be saved together (e.g., creating a TodoItem with its initial TodoStatusHistory).

**Business Value:**
- Ensures data consistency across multi-entity operations
- Prevents partial saves when operations fail mid-way
- Centralizes audit field management
- Provides explicit transaction boundaries

---

## Problem Statement

### Current Issue
Without a Unit of Work abstraction, multi-step save operations risk data inconsistency:

```csharp
// Problem: If second save fails, first save is already committed
var todoItem = new TodoItem { ... };
_dbContext.TodoItems.Add(todoItem);
await _dbContext.SaveChangesAsync(ct);  // ← Committed

var statusHistory = new TodoStatusHistory { ... };
_dbContext.TodoStatusHistory.Add(statusHistory);
await _dbContext.SaveChangesAsync(ct);  // ← If fails, data is inconsistent!
```

### Impact
- Data integrity violations
- Incomplete workflows in partial failures
- No rollback mechanism if operations fail mid-sequence

---

## Solution Design

### Architecture
```
Module (API Endpoint)
    ↓
Handler (MediatR)
    ↓
Application Service
    ├─ Begin Transaction (UoW)
    ├─ Orchestrate business logic
    ├─ Add/modify multiple entities
    └─ Commit atomically (UoW)
        ↓
Unit of Work (Transaction Manager)
    ├─ Exposes DbContext
    ├─ Manages transactions
    └─ Sets audit fields
        ↓
DbContext (EF Core)
```

### Key Design Decisions

**Decision 1: Expose DbContext Directly**
- Unit of Work is NOT a Repository pattern
- Application Service uses DbContext directly
- Only manages transaction boundaries, not data abstraction

**Decision 2: Transaction Management**
- BeginTransactionAsync() starts database transaction
- CommitTransactionAsync() saves all changes atomically
- RollbackTransactionAsync() reverts all changes
- All-or-nothing semantics

**Decision 3: Audit Field Handling**
- SaveChangesAsync(userId) automatically sets audit fields
- Centralizes CreatedBy/UpdatedBy/DeletedBy logic

---

## Implementation Details

### New Files to Create

#### 1. Api/Interfaces/IUnitOfWork.cs
```csharp
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Interfaces;

/// <summary>
/// Unit of Work pattern for managing atomic transactions across multiple entities.
/// Provides transaction boundaries and audit field management.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Direct access to DbContext for queries and entity operations.
    /// Application Service should use this for both queries and adds/updates.
    /// </summary>
    AppDbContext DbContext { get; }

    /// <summary>
    /// Begin a database transaction. All subsequent SaveChanges will be atomic.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commit the transaction. Saves all changes atomically, then commits transaction.
    /// Throws if SaveChanges fails (automatic rollback occurs).
    /// </summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Rollback the transaction. Reverts all changes made since BeginTransactionAsync.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Save changes without populating audit fields.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Save changes and automatically set CreatedBy/UpdatedBy fields for auditable entities.
    /// </summary>
    Task<int> SaveChangesAsync(string userId, CancellationToken ct = default);
}
```

#### 2. Api/Data/UnitOfWork.cs (Implementation)
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Api.Interfaces;
using Api.Entities.Base;

namespace Api.Data;

/// <summary>
/// Implementation of Unit of Work pattern for managing atomic transactions.
/// Provides transaction boundaries and automatic audit field management.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public AppDbContext DbContext => _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Begin a database transaction. All subsequent operations will be atomic.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        return _transaction;
    }

    /// <summary>
    /// Commit the transaction. Saves all changes and commits the transaction.
    /// If SaveChanges fails, automatically rolls back.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
            }
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rollback the transaction. Reverts all changes made since BeginTransactionAsync.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Save changes without audit field management.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Save changes and automatically populate CreatedBy/UpdatedBy fields.
    /// </summary>
    public async Task<int> SaveChangesAsync(string userId, CancellationToken ct = default)
    {
        SetAuditFields(userId);
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Automatically set audit fields (CreatedBy, UpdatedBy) for auditable entities.
    /// </summary>
    private void SetAuditFields(string userId)
    {
        var entries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && 
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedBy = userId;
            }
            entity.UpdatedBy = userId;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
    }
}
```

### Program.cs Registration
Add to DI container:
```csharp
// Dependency Injection - Data Layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### Usage Example: Application Service

```csharp
// Api/Services/Visits/TodoService.cs
using Api.Interfaces;
using Api.Data;
using Api.Exceptions;
using Api.Entities.Visits;
using Api.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Visits;

public class TodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VisitDomainService _domainService;

    public TodoService(
        IUnitOfWork unitOfWork,
        VisitDomainService domainService)
    {
        _unitOfWork = unitOfWork;
        _domainService = domainService;
    }

    public async Task<TodoItem> CreateTodoItemAsync(
        string mrId,
        string hcoId,
        CancellationToken ct)
    {
        try
        {
            // Start atomic transaction boundary
            await _unitOfWork.BeginTransactionAsync(ct);

            // Validate preconditions
            var member = await _unitOfWork.DbContext.Members
                .FirstOrDefaultAsync(m => m.Id == mrId && !m.IsDeleted, ct);
            if (member == null)
                throw new NotFoundException("Medical representative not found");

            var workspace = await _unitOfWork.DbContext.Workspaces
                .FirstOrDefaultAsync(h => h.Id == hcoId && !h.IsDeleted, ct);
            if (workspace == null)
                throw new NotFoundException("Healthcare organization not found");

            // Apply business rules
            if (!_domainService.IsRepresentativeAuthorizedForOrg(member, hco))
                throw new UnauthorizedException("Medical representative not authorized for this organization");

            // Create entities (both added, but not yet committed)
            var todoItem = new TodoItem
            {
                MrId = mrId,
                HcoId = hcoId,
                CurrentStatus = "Requested"
            };
            _unitOfWork.DbContext.TodoItems.Add(todoItem);

            var statusHistory = new TodoStatusHistory
            {
                TodoItemId = todoItem.Id,
                Status = "Requested",
                Timestamp = DateTime.UtcNow
            };
            _unitOfWork.DbContext.TodoStatusHistory.Add(statusHistory);

            // Commit both atomically - either both succeed or both roll back
            await _unitOfWork.CommitTransactionAsync(ct);

            return todoItem;
        }
        catch (Exception ex)
        {
            // Automatic rollback on any exception
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
```

---

## Testing Strategy

### Unit Tests
```csharp
// Api.Tests/Data/UnitOfWorkTests.cs
using Api.Data;
using Api.Entities.Core;
using Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Data;

public class UnitOfWorkTests : IAsyncLifetime
{
    private AppDbContext _dbContext;
    private IUnitOfWork _unitOfWork;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("test_" + Guid.NewGuid())
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    [Fact]
    public async Task CommitTransaction_WithMultipleAdds_SavesAll()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var member = new Member 
        { 
            Name = "John", 
            EmployeeId = "emp1", 
            Email = "john@test.com" 
        };
        _unitOfWork.DbContext.Members.Add(mr);

        var workspace = new Workspace 
        { 
            Name = "Hospital",
            RegionId = "region1"
        };
        _unitOfWork.DbContext.Workspaces.Add(hco);

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var savedMr = await _dbContext.Members
            .FirstOrDefaultAsync(m => m.Name == "John");
        var savedHco = await _dbContext.Workspaces
            .FirstOrDefaultAsync(h => h.Name == "Hospital");

        Assert.NotNull(savedMr);
        Assert.NotNull(savedHco);
    }

    [Fact]
    public async Task RollbackTransaction_WithMultipleAdds_DiscadsAll()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var member = new Member 
        { 
            Name = "John", 
            EmployeeId = "emp1", 
            Email = "john@test.com" 
        };
        _unitOfWork.DbContext.Members.Add(mr);

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert - should not exist
        var result = await _dbContext.Members
            .FirstOrDefaultAsync(m => m.Name == "John");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithUserId_PopulatesAuditFields()
    {
        // Arrange
        var member = new Member 
        { 
            Name = "John", 
            EmployeeId = "emp1", 
            Email = "john@test.com" 
        };
        _unitOfWork.DbContext.Members.Add(mr);

        // Act
        await _unitOfWork.SaveChangesAsync("user123");

        // Assert
        var saved = await _dbContext.Members
            .FirstOrDefaultAsync(m => m.Name == "John");
        Assert.Equal("user123", saved.CreatedBy);
        Assert.Equal("user123", saved.UpdatedBy);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}
```

### Integration Tests
- Test complete Application Service workflows with real database
- Verify atomic commit/rollback behavior
- Test audit field population in production scenario

---

## Acceptance Criteria

- [ ] IUnitOfWork interface created in Api/Interfaces/
- [ ] UnitOfWork implementation created in Api/Data/
- [ ] DI registration added to Program.cs
- [ ] All unit tests pass (transaction atomicity verified)
- [ ] Integration test created for Application Service workflow
- [ ] Code review approved
- [ ] Architecture standards documentation updated

---

## Files Created/Modified

| File | Change | Type |
|------|--------|------|
| Api/Interfaces/IUnitOfWork.cs | Create | New interface |
| Api/Data/UnitOfWork.cs | Create | Implementation |
| Program.cs | Add DI registration | Modification |
| Api.Tests/Data/UnitOfWorkTests.cs | Create | Unit tests |

---

## Dependencies

- Microsoft.EntityFrameworkCore (already installed)
- Api.Entities.Base.IAuditableEntity (already exists)
- Api.Data.AppDbContext (already exists)

---

## Related Standards & Documentation

- Historical architecture notes from the original fork were removed during OpenSpec cleanup.

---

## Implementation Timeline

- **Day 1:** Create IUnitOfWork and UnitOfWork classes
- **Day 2:** Write unit tests, register in DI
- **Day 3:** Integration tests and code review

---

## Notes

- Unit of Work does NOT replace Repository pattern (which we're avoiding)
- Exposes DbContext directly (no abstraction layer)
- Pure transaction boundary management only
- Works seamlessly with Application Service pattern
- Centralizes audit field logic in one place

---

## Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-02-04 | Architecture Team | Initial proposal |
