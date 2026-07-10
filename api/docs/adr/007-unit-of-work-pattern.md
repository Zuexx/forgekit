# ADR-007: Unit of Work Pattern for Transaction Management

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-002, ADR-004, ADR-005

## Context

Database transactions are critical for data consistency, but managing them correctly is challenging:

1. **Atomicity:** Multiple operations must all succeed or all fail together
2. **Consistency:** Related entities must be saved as a unit
3. **Distributed Changes:** Changes to multiple entities should be tracked together
4. **Rollback Safety:** If something fails, all changes are reverted
5. **Audit Trail:** Who made what changes and when
6. **Service Coordination:** Multiple services modifying data in same operation

Example: Creating a visit request should:
- Create TodoItem entity
- Create TodoStatusHistory entity (related)
- Both succeed or both fail (atomic)
- Both tracked with same CreatedBy user
- All reverted if validation fails partway through

## Decision

Implement **Unit of Work Pattern** where:

1. **IUnitOfWork Interface:** Exposes DbContext and transaction methods
2. **Scoped Lifetime:** One instance per request/operation
3. **DbContext Access:** Services access `IUnitOfWork.DbContext` directly for queries
4. **Transaction Methods:** BeginTransaction, CommitTransaction, RollbackTransaction
5. **Automatic Audit Population:** SaveChangesAsync(userId) populates CreatedBy/UpdatedBy
6. **Fail-Safe Rollback:** Automatic rollback on SaveChanges failure

### IUnitOfWork Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Direct access to DbContext for queries and entity operations.
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
    /// Save changes and automatically set CreatedBy/UpdatedBy fields.
    /// </summary>
    Task<int> SaveChangesAsync(string userId, CancellationToken ct = default);
}
```

## Rationale

### Why Unit of Work Pattern?

1. **Transaction Boundaries:**
   - Clear start: `BeginTransactionAsync()`
   - Clear end: `CommitTransactionAsync()` or `RollbackTransactionAsync()`
   - All operations between are atomic

2. **Atomicity Guarantee:**
   - Multiple entities modified together
   - Either all succeed or all fail (no partial updates)
   - Example: TodoItem + TodoStatusHistory always saved together

3. **Automatic Audit Field Management:**
   ```csharp
   // Don't set CreatedBy/UpdatedBy manually
   await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);
   // IUnitOfWork sets CreatedBy and UpdatedBy automatically
   ```

4. **Consistent Data State:**
   - All entities in transaction see same state
   - No dirty reads between operations
   - Data integrity guaranteed

5. **Rollback Safety:**
   - If any operation fails, everything reverts
   - No orphaned records (e.g., TodoItem without TodoStatusHistory)
   - Database always in valid state

6. **Single Responsibility:**
   - Services focus on business logic
   - IUnitOfWork manages transactions
   - Clear separation of concerns

7. **Testability:**
   - Mock IUnitOfWork for unit tests
   - Test with real DbContext in integration tests
   - Easy to verify SaveChangesAsync called with correct userId

8. **DRY Principle:**
   - Audit field population in one place (IUnitOfWork.SaveChangesAsync)
   - No developers forget to set audit fields
   - Consistent behavior everywhere

## Alternatives Considered

### 1. No Transaction Management (Fire and Forget)
**Approach:** Save each entity individually without transactions

```csharp
public async Task CreateTodoItem(string mrId, string hcoId, DateTime visitDate)
{
    var todoItem = new TodoItem { MrId = mrId, HcoId = hcoId, VisitDate = visitDate };
    _dbContext.Add(todoItem);
    await _dbContext.SaveChangesAsync(); // Save visit

    var statusHistory = new TodoStatusHistory { TodoItemId = todoItem.Id, Status = "Requested" };
    _dbContext.Add(statusHistory);
    await _dbContext.SaveChangesAsync(); // Save status history separately
}
```

**Pros:**
- Simple, no transaction overhead
- Obvious what's happening

**Cons:**
- Not atomic (TodoItem saved but status history fails → orphan record)
- Database in inconsistent state if failure between saves
- No rollback capability
- Data integrity not guaranteed
- Violates domain rules (visit without status history)
- Difficult to debug partial failures

**When Better:** Only for read-only operations; logging/metrics where atomicity doesn't matter

---

### 2. DbContext.Database.BeginTransactionAsync Directly
**Approach:** Services call DbContext transaction methods directly

```csharp
public async Task CreateTodoItem(string mrId, string hcoId, DateTime visitDate)
{
    var tx = await _dbContext.Database.BeginTransactionAsync();
    try
    {
        var todoItem = new TodoItem { MrId = mrId, HcoId = hcoId, VisitDate = visitDate };
        _dbContext.Add(todoItem);

        var statusHistory = new TodoStatusHistory { TodoItemId = todoItem.Id };
        _dbContext.Add(statusHistory);

        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
    finally
    {
        await tx.DisposeAsync();
    }
}
```

**Pros:**
- Full control over transactions
- Explicit about what's transactional
- No abstraction layer

**Cons:**
- Boilerplate in every service (try/catch/finally)
- Easy to forget rollback
- Difficult to test (mock DbContext.Database)
- Couples services directly to DbContext
- No audit field automation
- Inconsistent across services (different implementations)
- Easy to misuse (forget Dispose, leave hanging transaction)

**Trade-off:** Explicit vs boilerplate; control vs complexity

**When Better:** Never better than Unit of Work (always adds value)

---

### 3. Ambient Transaction Context
**Approach:** Use ambient transaction scope (System.Transactions)

```csharp
using (var scope = new TransactionScope())
{
    var todoItem = new TodoItem { /* ... */ };
    _dbContext.Add(todoItem);
    await _dbContext.SaveChangesAsync();

    var statusHistory = new TodoStatusHistory { /* ... */ };
    _dbContext.Add(statusHistory);
    await _dbContext.SaveChangesAsync();

    scope.Complete();
}
```

**Pros:**
- Automatic transaction handling
- Works with multiple data sources
- Less boilerplate than manual transaction

**Cons:**
- Requires database/provider support for distributed transactions
- Compatibility issues with async/await (TransactionScope not async-friendly)
- Hidden transaction scope (implicit)
- Hard to test (ambient context is global)
- Not recommended for async code
- Difficult to control transaction boundaries
- Performance overhead for distributed transactions

**When Better:** Legacy code; Windows MSDTC scenarios (rare)

---

### 4. Aggregate Root Transactions
**Approach:** Each aggregate handles its own transaction

```csharp
public class TodoItem : AggregateRoot
{
    private IUnitOfWork _unitOfWork;

    public async Task CreateAsync(string mrId, string hcoId, DateTime visitDate)
    {
        var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Add related status history
            AddStatusHistory(new TodoStatusHistory { /* ... */ });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}

// Service usage
var todoItem = new TodoItem();
await todoItem.CreateAsync(mrId, hcoId, visitDate);
```

**Pros:**
- Encapsulates transaction logic in domain
- Clear responsibility boundary
- Testable aggregate behavior

**Cons:**
- Aggregate depends on IUnitOfWork (couples domain to infrastructure)
- Multiple aggregates can't participate in single transaction
- Complex coordinated operations require helper services anyway
- Mixing domain logic with transaction logic
- Increases coupling

**When Better:** Pure Domain-Driven Design; simple aggregates with few operations

---

### 5. Saga Pattern for Distributed Transactions
**Approach:** Use sagas for multi-service transactions

```csharp
public class CreateTodoItemSaga : Saga<CreateTodoItemSagaState>
{
    public async Task Handle(CreateTodoItemCommand command)
    {
        var todoItem = await _todoService.CreateAsync(command);

        await _notificationService.SendAsync(new VisitCreatedNotification(todoItem.Id));

        await _analyticsService.RecordAsync(new VisitCreatedEvent(todoItem.Id));
    }

    // Compensating actions for rollback
    public async Task Compensate()
    {
        await _visitService.DeleteAsync(todoItem.Id);
        await _notificationService.UndoAsync(notification.Id);
        await _analyticsService.UndoAsync(analyticsRecord.Id);
    }
}
```

**Pros:**
- Supports distributed transactions across services
- Compensating actions define rollback behavior
- Scalable for microservices

**Cons:**
- Over-engineered for monolith
- Requires saga orchestration/choreography framework
- Complex to implement and test
- Overkill for simple atomic operations
- Not suitable for monolithic application
- Adds significant complexity

**When Better:** Microservices with separate databases; distributed transactions

---

## Consequences

### Positive

1. **Atomicity Guaranteed:** Related changes all succeed or all fail
2. **Data Consistency:** Database always in valid state
3. **Automatic Rollback:** Failures automatically revert changes
4. **Audit Trail:** Automatic CreatedBy/UpdatedBy population
5. **Clear Boundaries:** Services don't manage transactions directly
6. **Testable:** Mock IUnitOfWork or test with real DbContext
7. **DRY Code:** Audit field logic in one place
8. **Standard Pattern:** Well-known, easy to understand
9. **Flexible:** Works with simple single-entity operations or complex multi-entity operations
10. **Safe Default:** Encourages correct transaction handling

### Negative

1. **Abstraction Overhead:** Additional interface and implementation
2. **Scoped Lifetime:** Services must be scoped (slight overhead)
3. **Exception Handling:** Services must handle rollback in catch blocks
4. **Disposal Required:** IUnitOfWork must be disposed properly
5. **Long-Lived Transactions:** Can cause lock contention if not careful

### Neutral

1. **Architectural Complexity:** Adds layer, but necessary layer
2. **Performance:** Transaction overhead minimal (same as direct DbContext.Database.BeginTransaction)

## When to Use

✅ **Use Unit of Work Pattern when:**
- Building application with multiple related entities
- Need atomic operations (all succeed or all fail)
- Want automatic audit field population
- Using EF Core with DbContext
- ASP.NET Core application with DI container
- Multiple entities modified in single operation
- Transactions important for data integrity
- Want consistent transaction handling across services

✅ **Specifically for:**
- Command handlers (data modification)
- Domain services (multi-entity operations)
- Business logic requiring atomicity

## When NOT to Use

❌ **Avoid Unit of Work Pattern when:**
- Read-only operations (queries don't need transactions)
- Single entity operations that don't need rollback
- Not using .NET/EF Core
- No DI container available
- Transaction overhead is critical bottleneck
- Distributed/saga transactions needed

❌ **Don't use for:**
- Query handlers (only read operations)
- Infrastructure services
- Simple CRUD operations without related entities

## ForgeKit Implementation

### IUnitOfWork Interface

```csharp
// ForgeKit.Api/Interfaces/IUnitOfWork.cs
using Api.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Interfaces;

/// <summary>
/// Unit of Work pattern for managing atomic transactions across multiple entities.
/// Provides transaction boundaries and automatic audit field management.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Direct access to DbContext for queries and entity operations.
    /// Application Service uses this for both queries and adds/updates.
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

### UnitOfWork Implementation

```csharp
// ForgeKit.Api/Data/UnitOfWork.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Api.Entities.Base;
using Api.Interfaces;

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
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Begin a database transaction. All subsequent SaveChanges will be atomic.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        return _transaction;
    }

    /// <summary>
    /// Commit the transaction. Saves all changes atomically, then commits transaction.
    /// If SaveChanges fails, automatically rolls back.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        try
        {
            // Save all pending changes to database
            await _dbContext.SaveChangesAsync(ct);

            // Commit the transaction
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
            }
        }
        catch
        {
            // Automatic rollback on save failure
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            // Clean up transaction resources
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
    /// Save changes without populating audit fields.
    /// Used for entities that don't have audit fields or when audit fields set manually.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Save changes and automatically populate CreatedBy/UpdatedBy fields for auditable entities.
    /// </summary>
    public async Task<int> SaveChangesAsync(string userId, CancellationToken ct = default)
    {
        SetAuditFields(userId);
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Automatically set audit fields (CreatedBy, UpdatedBy) for entities implementing IAuditableEntity.
    /// </summary>
    private void SetAuditFields(string userId)
    {
        var entries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;

            // Set CreatedBy only on new entities
            if (entry.State == EntityState.Added)
            {
                entity.CreatedBy = userId;
            }

            // Set UpdatedBy on both new and modified entities
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

### Service Usage Example

```csharp
// ForgeKit.Api/Services/Visits/TodoService.cs
public class TodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditContext _auditContext;
    private readonly SoftDeleteDomainService _softDeleteService;

    public async Task<TodoItem> CreateTodoAsync(
        string mrId,
        string hcoId,
        DateTime visitDate,
        string? purpose = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mrId))
            throw new ArgumentException("MR ID cannot be empty");

        if (string.IsNullOrWhiteSpace(hcoId))
            throw new ArgumentException("HCO ID cannot be empty");

        if (visitDate < DateTime.UtcNow.Date)
            throw new BusinessLogicException("Visit date must be in the future");

        // Begin transaction for atomic operation
        var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            // Validate MR exists
            var member = await _unitOfWork.DbContext.Set<Entities.Core.Member>()
                .FirstOrDefaultAsync(m => m.Id == mrId, cancellationToken: ct);

            if (member == null)
                throw new BusinessLogicException($"Medical Representative '{mrId}' not found");

            // Validate HCO exists
            var workspace = await _unitOfWork.DbContext.Set<Entities.Core.Workspace>()
                .FirstOrDefaultAsync(h => h.Id == hcoId, cancellationToken: ct);

            if (workspace == null)
                throw new BusinessLogicException($"Healthcare Organization '{hcoId}' not found");

            // Create visit request
            var todoItem = new TodoItem
            {
                MrId = mrId,
                HcoId = hcoId,
                VisitDate = visitDate,
                Purpose = purpose,
                CurrentStatus = "Requested"
            };

            _unitOfWork.DbContext.Add(todoItem);

            // Create initial status history
            var statusHistory = new TodoStatusHistory
            {
                TodoItemId = todoItem.Id,
                Status = "Requested",
                Timestamp = _auditContext.UtcNow,
                ChangedBy = _auditContext.UserId,
                Notes = "Visit request created"
            };

            _unitOfWork.DbContext.Add(statusHistory);

            // Save both entities atomically with audit fields
            // IUnitOfWork automatically sets CreatedBy and UpdatedBy
            await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);

            // Commit the transaction (both entities saved or both rolled back)
            await _unitOfWork.CommitTransactionAsync(ct);

            return todoItem;
        }
        catch
        {
            // Automatic rollback if anything fails
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            // Clean up transaction resources
            await transaction.DisposeAsync();
        }
    }

    public async Task<TodoItem> ApproveTodoItemAsync(
        string todoItemId,
        CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var todoItem = await _unitOfWork.DbContext.Set<TodoItem>()
                .FirstOrDefaultAsync(v => v.Id == todoItemId, cancellationToken: ct);

            if (todoItem == null)
                throw new BusinessLogicException($"TodoItem '{todoItemId}' not found");

            if (todoItem.CurrentStatus != "Requested")
                throw new BusinessLogicException(
                    $"Can only approve 'Requested' visits, current status: {todoItem.CurrentStatus}");

            // Update visit status
            todoItem.CurrentStatus = "Approved";

            // Add status history
            var statusHistory = new TodoStatusHistory
            {
                TodoItemId = todoItemId,
                Status = "Approved",
                Timestamp = _auditContext.UtcNow,
                ChangedBy = _auditContext.UserId,
                Notes = "Visit request approved"
            };

            _unitOfWork.DbContext.Add(statusHistory);

            // Save atomic
            await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return todoItem;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }
}
```

### Unit Test

```csharp
// ForgeKit.Api.Tests/Data/UnitOfWorkTests.cs
[TestFixture]
public class UnitOfWorkTests
{
    private AppDbContext _dbContext;
    private UnitOfWork _unitOfWork;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    [Test]
    public async Task SaveChangesAsync_WithUserId_PopulatesAuditFields()
    {
        // Arrange
        var entity = new TodoItem
        {
            MrId = "mr-1",
            HcoId = "hco-1",
            VisitDate = DateTime.UtcNow.AddDays(5)
        };

        _unitOfWork.DbContext.Add(entity);

        // Act
        await _unitOfWork.SaveChangesAsync("test-user-123");

        // Assert
        Assert.That(entity.CreatedBy, Is.EqualTo("test-user-123"));
        Assert.That(entity.UpdatedBy, Is.EqualTo("test-user-123"));
    }

    [Test]
    public async Task CommitTransaction_SavesAllChangesAtomically()
    {
        // Arrange
        var tx = await _unitOfWork.BeginTransactionAsync();

        var visit = new TodoItem
        {
            MrId = "mr-1",
            HcoId = "hco-1",
            VisitDate = DateTime.UtcNow.AddDays(5)
        };

        var status = new TodoStatusHistory
        {
            Status = "Requested",
            Timestamp = DateTime.UtcNow
        };

        _unitOfWork.DbContext.Add(visit);
        _unitOfWork.DbContext.Add(status);

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var savedVisit = await _dbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == visit.Id);
        var savedStatus = await _dbContext.Set<TodoStatusHistory>()
            .FirstOrDefaultAsync(s => s.Id == status.Id);

        Assert.That(savedVisit, Is.Not.Null);
        Assert.That(savedStatus, Is.Not.Null);

        await tx.DisposeAsync();
    }

    [Test]
    public async Task RollbackTransaction_Reverts AllChanges()
    {
        // Arrange
        var tx = await _unitOfWork.BeginTransactionAsync();

        var visit = new TodoItem
        {
            MrId = "mr-1",
            HcoId = "hco-1",
            VisitDate = DateTime.UtcNow.AddDays(5)
        };

        _unitOfWork.DbContext.Add(visit);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var notFound = await _dbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == visit.Id);

        Assert.That(notFound, Is.Null);

        await tx.DisposeAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _unitOfWork?.Dispose();
        _dbContext?.Dispose();
    }
}
```

### Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ... rest of configuration
```

## ForgeKit-Specific Transaction Patterns

### Multi-Step Approval Workflow

```csharp
public async Task ApproveAndNotifyAsync(string todoItemId, CancellationToken ct)
{
    var tx = await _unitOfWork.BeginTransactionAsync(ct);
    try
    {
        // Step 1: Update visit
        var visit = await _unitOfWork.DbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == todoItemId, ct);
        visit.CurrentStatus = "Approved";

        // Step 2: Add status history
        var history = new TodoStatusHistory { /* ... */ };
        _unitOfWork.DbContext.Add(history);

        // Step 3: Add notification
        var notification = new Notification { /* ... */ };
        _unitOfWork.DbContext.Add(notification);

        // All or nothing
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);
        await _unitOfWork.CommitTransactionAsync(ct);
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
    finally
    {
        await tx.DisposeAsync();
    }
}
```

## Related ADRs

- **ADR-002:** Soft-delete pattern (uses IUnitOfWork for atomic deletes/restores)
- **ADR-004:** Direct DbContext usage (IUnitOfWork exposes DbContext)
- **ADR-005:** Audit context service (IUnitOfWork.SaveChangesAsync uses IAuditContext)

## References

- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Entity Framework Core DbContext](https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF Core Transactions](https://docs.microsoft.com/en-us/ef/core/saving/transactions)
- [ASP.NET Core Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
