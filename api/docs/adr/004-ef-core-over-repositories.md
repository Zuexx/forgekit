# ADR-004: Direct DbContext Usage Over Repository Pattern

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-001, ADR-007

## Context

Data access layer design is fundamental to application architecture. Traditional approaches include:

1. **Repository Pattern:** Abstraction layer between business logic and data access
2. **Direct DbContext:** Expose DbContext directly to application services
3. **ORM-Specific Abstractions:** LINQ, IQueryable exposed directly

ForgeKit requirements:
- Access to full DbContext power (LINQ, eager loading, query composition)
- Avoid repository boilerplate for common operations
- Enable complex queries without repository method explosion
- Support database transactions for atomic multi-aggregate operations
- Maintain testability without repository mocking

## Decision

Use **Direct DbContext Access** (via IUnitOfWork.DbContext) where:

1. **IUnitOfWork exposes DbContext:** `AppDbContext DbContext { get; }`
2. **Application Services access DbContext directly:** `_unitOfWork.DbContext.Set<T>()`
3. **Services handle all queries and mutations:** No separate repository classes
4. **Unit of Work manages transactions:** Services use IUnitOfWork for atomic operations
5. **Tests mock IUnitOfWork, not repositories:** Easier to test with real DbContext behavior

### Unit of Work Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    AppDbContext DbContext { get; }
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(string userId, CancellationToken ct = default);
}
```

## Rationale

### Why Direct DbContext Over Repository?

1. **Simplicity:**
   - No abstraction layer to maintain
   - Developers use DbContext directly (what they're learning anyway)
   - Less boilerplate code
   - Faster to write new services

2. **Full LINQ Power:**
   - Can use all LINQ operators without wrapping
   - Projections, grouping, complex joins work naturally
   - No "repository method explosion" (CreateRepository, GetActiveByDateRange, GetWithDetails, etc.)
   - Example:
     ```csharp
     var results = _unitOfWork.DbContext.Set<TodoItem>()
         .Where(v => v.VisitDate >= DateTime.Today)
         .GroupBy(v => v.CurrentStatus)
         .Select(g => new { Status = g.Key, Count = g.Count() })
         .ToListAsync(ct);
     ```

3. **Query Composition:**
   - Build queries dynamically without helper methods
   - Stack conditions without repository method creation
   - Filter, order, include naturally:
     ```csharp
     var query = _unitOfWork.DbContext.Set<TodoItem>().AsQueryable();

     if (!string.IsNullOrEmpty(mrId))
         query = query.Where(v => v.MrId == mrId);

     if (includeDetails)
         query = query.Include(v => v.Member)
                     .Include(v => v.Workspace);

     return await query.ToListAsync(ct);
     ```

4. **Lazy Loading and Eager Loading Control:**
   - Include() for eager loading
   - Select() for projections
   - No hidden queries or N+1 problems
   - Developers see LINQ and understand query execution

5. **Transaction Support:**
   - Direct access to transactions via DbContext.Database
   - Atomic operations across multiple entities easily
   - Unit of Work manages transaction boundaries
   - Example:
     ```csharp
     var tx = await _unitOfWork.BeginTransactionAsync();
     try
     {
         _unitOfWork.DbContext.Add(entity1);
         _unitOfWork.DbContext.Add(entity2);
         await _unitOfWork.SaveChangesAsync(userId, ct);
         await _unitOfWork.CommitTransactionAsync();
     }
     catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
     ```

6. **Testing Flexibility:**
   - Can test with in-memory database (DbContextOptions with InMemory provider)
   - Can test with real database (SQLite in-memory)
   - Mocking IUnitOfWork is straightforward
   - Tests see exactly what database sees
   - Example:
     ```csharp
     [Test]
     public async Task CreateTodoItem_SavesCorrectly()
     {
         var options = new DbContextOptionsBuilder<AppDbContext>()
             .UseInMemoryDatabase("test")
             .Options;

         var dbContext = new AppDbContext(options);
         var unitOfWork = new UnitOfWork(dbContext);
         var service = new TodoService(unitOfWork, ...);

         var result = await service.CreateTodoAsync(...);

         var saved = await dbContext.Set<TodoItem>()
             .FirstOrDefaultAsync(v => v.Id == result.Id);
         Assert.That(saved, Is.Not.Null);
     }
     ```

7. **Maintainability:**
   - Less code to maintain (no repository layer)
   - Developers use DbContext everywhere in .NET
   - Standard EF Core patterns
   - Easier to onboard new developers

8. **Performance:**
   - No repository method abstraction overhead
   - Direct DbContext calls avoid extra indirection
   - Developers see performance implications of queries
   - Can use database-specific features if needed

### Why NOT Repository Pattern?

The Repository Pattern was designed for:
- **Pre-LINQ era:** When LINQ didn't exist
- **Multiple data sources:** Swap implementations (SQL → XML → REST)
- **Data access abstraction:** Hide database details

Modern LINQ/EF Core make Repository Pattern obsolete:
- LINQ is already the abstraction
- Entity Framework already abstracts database details
- Swapping implementations is rare (when it happens, swap DbContext, not repositories)
- Repository methods become LINQ queries (just in a different layer)
- Repository pattern adds abstraction without benefits

**"Repository Pattern is deprecated with LINQ" - Microsoft Data Access Guidance**

## Alternatives Considered

### 1. Traditional Repository Pattern
**Approach:** Create IVisitRepository with methods like GetById, Create, Update, Delete

```csharp
public interface IVisitRepository
{
    Task<TodoItem> GetByIdAsync(string id);
    Task<List<TodoItem>> GetAllAsync();
    Task<List<TodoItem>> GetByMrIdAsync(string mrId);
    Task AddAsync(TodoItem entity);
    Task UpdateAsync(TodoItem entity);
    Task DeleteAsync(string id);
}

public class VisitRepository : IVisitRepository
{
    private readonly AppDbContext _dbContext;

    public async Task<TodoItem> GetByIdAsync(string id) =>
        await _dbContext.Set<TodoItem>().FirstOrDefaultAsync(v => v.Id == id);

    public async Task<List<TodoItem>> GetAllAsync() =>
        await _dbContext.Set<TodoItem>().ToListAsync();

    public async Task<List<TodoItem>> GetByMrIdAsync(string mrId) =>
        await _dbContext.Set<TodoItem>()
            .Where(v => v.MrId == mrId)
            .ToListAsync();

    // ... more methods
}
```

**Pros:**
- Encapsulates data access
- Explicit about available operations
- Easy to swap implementations
- Common in traditional architectures

**Cons:**
- Repository method explosion (GetByMrId, GetByHcoId, GetByStatusAndMrId, etc.)
- Duplicate LINQ logic across repositories
- Can't compose queries (must have GetByDateRangeAndStatus method)
- Abstraction without benefit (already have LINQ)
- Extra layer to maintain
- Tests must mock repository interfaces
- Developers learn LINQ but use repository method names

**When Better:** Multiple data sources (SQL + file system); strict data access encapsulation requirements

---

### 2. Generic Repository Pattern
**Approach:** Create generic IRepository<T> to reduce boilerplate

```csharp
public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> GetAll();
    Task<T> GetByIdAsync(string id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<T> _dbSet;

    public IQueryable<T> GetAll() => _dbSet.AsQueryable();

    public async Task<T> GetByIdAsync(string id) =>
        await _dbSet.FirstOrDefaultAsync(e => e.Id == id);

    public async Task AddAsync(T entity) =>
        _dbSet.Add(entity);

    // ... other methods
}

// Usage
var todoItems = await _todoRepository.GetAll()
    .Where(v => v.Status == "Approved")
    .ToListAsync();
```

**Pros:**
- Less boilerplate than custom repositories
- Consistent CRUD operations
- Can still use GetAll().Where() for queries
- Reusable across all entities

**Cons:**
- Still adds abstraction layer
- GetAll() returns IQueryable (leaks LINQ details anyway)
- Doesn't prevent repository method explosion (developers add custom methods)
- Mock IRepository in tests instead of using real DbContext
- False sense of abstraction (still LINQ underneath)

**Trade-off:** Balance between abstraction and practicality

**When Better:** Team prefers abstraction for abstraction's sake; large teams needing consistency

---

### 3. Specification Pattern
**Approach:** Use specification objects to encapsulate query criteria

```csharp
public abstract class BaseSpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>> OrderBy { get; protected set; }
    public int Take { get; protected set; }
    public int Skip { get; protected set; }
    public bool IsPagingEnabled { get; protected set; }
}

public class GetVisitsByMrSpecification : BaseSpecification<TodoItem>
{
    public GetVisitsByMrSpecification(string mrId)
    {
        Criteria = v => v.MrId == mrId && !v.IsDeleted;
        Includes.Add(v => v.Member);
        OrderBy = v => v.VisitDate;
    }
}

// Usage
var spec = new GetVisitsByMrSpecification(mrId);
var visits = await _specificationRepository.GetAsync(spec);
```

**Pros:**
- Encapsulates complex query logic
- Reusable across services
- Clear intent (specification name describes query)
- Testable specifications independently

**Cons:**
- Requires Evaluatrix/specification framework
- Adds abstraction layer
- Boilerplate for each query type
- Loses IDE LINQ intellisense
- Over-engineered for simple queries
- Still require ISpecificationRepository

**When Better:** Very complex queries; query reuse across multiple services; legacy codebase

---

### 4. CQRS with Query Objects
**Approach:** Separate commands from queries with explicit query handlers

```csharp
public class GetVisitsByMrQuery : IRequest<List<TodoItem>>
{
    public string MrId { get; set; }
}

public class GetVisitsByMrQueryHandler : IRequestHandler<GetVisitsByMrQuery, List<TodoItem>>
{
    private readonly AppDbContext _dbContext;

    public async Task<List<TodoItem>> Handle(GetVisitsByMrQuery request, CancellationToken ct)
    {
        return await _dbContext.Set<TodoItem>()
            .Where(v => v.MrId == request.MrId && !v.IsDeleted)
            .Include(v => v.Member)
            .OrderBy(v => v.VisitDate)
            .ToListAsync(ct);
    }
}

// Usage
var visits = await mediator.Send(new GetVisitsByMrQuery { MrId = mrId });
```

**Pros:**
- Clean separation of concerns
- Queries are type-safe
- Easy to add validation, logging, caching
- Testable query handlers
- Can cache entire query result

**Cons:**
- Handler method per query type
- More code than direct DbContext
- Requires MediatR infrastructure
- Over-engineered for simple queries
- Can make ad-hoc queries harder

**Trade-off:** Explicit vs implicit; full CQRS vs simple queries

**When Better:** Complex domain; different authorization per query; query-side optimization critical

---

## Consequences

### Positive

1. **Less Code:** No repository layer means fewer files to maintain
2. **Full LINQ Access:** Can use all EF Core features naturally
3. **Dynamic Queries:** Build queries without predefined repository methods
4. **Clear Intent:** LINQ syntax is explicit about what data is fetched
5. **Better Performance:** No abstraction overhead, developers see query costs
6. **Easier Testing:** Test with real DbContext (in-memory or SQLite)
7. **Familiar Patterns:** All .NET developers know DbContext and LINQ
8. **Fast Development:** Less boilerplate to write new features

### Negative

1. **DbContext Coupling:** Services tightly coupled to EF Core
2. **Query Visibility:** Queries scattered across services (harder to find all queries)
3. **Inconsistency Risk:** Different services query differently (N+1 problems, missing includes)
4. **Testing Complexity:** Tests need real DbContext setup (more setup code)
5. **Learning Curve:** Developers must understand LINQ, DbContext, query execution
6. **Refactoring Difficulty:** Moving query logic is harder without repository abstraction

### Neutral

1. **Architectural Simplicity:** Reduces complexity vs Repository, but increases data access visibility
2. **Testability Trade-off:** Real DbContext testing is more realistic but requires more setup

## When to Use

✅ **Use Direct DbContext when:**
- Modern .NET application with EF Core (3.1+)
- Don't need to swap data source implementations
- Want to leverage full LINQ/EF Core capabilities
- Team is comfortable with DbContext and LINQ
- Need complex queries that don't fit repository pattern
- Database is primary/only data source
- Want less code and boilerplate
- Building business application (not framework)

✅ **Specifically for:**
- Application services accessing data
- Complex queries with multiple conditions/joins
- Any new .NET project (standard approach)

## When NOT to Use

❌ **Avoid Direct DbContext when:**
- Multiple data sources (SQL, file, REST APIs)
- Need strict data access abstraction
- Absolute requirement for implementation swapability
- Working with legacy Repository-based codebase
- Team insists on Repository pattern
- Code needs to work with non-EF data access
- Data access layer must be pluggable

❌ **Don't use for:**
- Infrastructure/framework code (use repositories/abstractions there)
- Cross-cutting concerns needing data access abstraction

## ForgeKit Implementation

### IUnitOfWork Interface (Data Access Abstraction)

```csharp
// ForgeKit.Api/Interfaces/IUnitOfWork.cs
using Api.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Interfaces;

/// <summary>
/// Unit of Work pattern for managing atomic transactions.
/// Provides transaction boundaries and automatic audit field management.
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

### Direct DbContext Access in Service

```csharp
// ForgeKit.Api/Services/Visits/TodoService.cs
using Api.Entities.Visits;
using Api.Exceptions;
using Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Visits
{
    /// <summary>
    /// Application Service for visit management.
    /// Uses direct DbContext access (via IUnitOfWork.DbContext) for queries and mutations.
    /// </summary>
    public class TodoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditContext _auditContext;
        private readonly SoftDeleteDomainService _softDeleteService;

        public TodoService(
            IUnitOfWork unitOfWork,
            IAuditContext auditContext,
            SoftDeleteDomainService softDeleteService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditContext = auditContext ?? throw new ArgumentNullException(nameof(auditContext));
            _softDeleteService = softDeleteService ?? throw new ArgumentNullException(nameof(softDeleteService));
        }

        /// <summary>
        /// Gets all visit requests for a specific Medical Representative.
        /// Demonstrates dynamic query composition with DbContext.
        /// </summary>
        public async Task<List<TodoItem>> GetVisitsByMrIdAsync(
            string mrId,
            bool includeDetails = true,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mrId))
                throw new ArgumentException("MR ID cannot be empty", nameof(mrId));

            // Direct DbContext access - compose query dynamically
            var query = _unitOfWork.DbContext.Set<TodoItem>()
                .Where(v => v.MrId == mrId)
                .AsQueryable();

            // Conditionally include related entities
            if (includeDetails)
            {
                query = query
                    .Include(v => v.Member)
                    .Include(v => v.Workspace)
                    .Include(v => v.StatusHistories);
            }

            // Order results
            query = query.OrderByDescending(v => v.VisitDate);

            return await query.ToListAsync(ct);
        }

        /// <summary>
        /// Gets visit statistics by status.
        /// Demonstrates LINQ grouping without repository method explosion.
        /// </summary>
        public async Task<Dictionary<string, int>> GetVisitStatisticsByStatusAsync(
            string mrId,
            CancellationToken ct = default)
        {
            return await _unitOfWork.DbContext.Set<TodoItem>()
                .Where(v => v.MrId == mrId)
                .GroupBy(v => v.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        }

        /// <summary>
        /// Creates a new visit request with related status history (atomic).
        /// Demonstrates multi-aggregate operation with transactions.
        /// </summary>
        public async Task<TodoItem> CreateTodoAsync(
            string mrId,
            string hcoId,
            DateTime visitDate,
            string? purpose = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mrId))
                throw new ArgumentException("MR ID cannot be empty", nameof(mrId));

            if (string.IsNullOrWhiteSpace(hcoId))
                throw new ArgumentException("HCO ID cannot be empty", nameof(hcoId));

            if (visitDate < DateTime.UtcNow.Date)
                throw new BusinessLogicException("Visit date must be in the future");

            var transaction = await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // Query 1: Validate MR exists
                var member = await _unitOfWork.DbContext.Set<Entities.Core.Member>()
                    .FirstOrDefaultAsync(m => m.Id == mrId, cancellationToken: ct);

                if (member == null)
                    throw new BusinessLogicException($"Medical Representative '{mrId}' not found");

                // Query 2: Validate HCO exists
                var workspace = await _unitOfWork.DbContext.Set<Entities.Core.Workspace>()
                    .FirstOrDefaultAsync(h => h.Id == hcoId, cancellationToken: ct);

                if (workspace == null)
                    throw new BusinessLogicException($"Healthcare Organization '{hcoId}' not found");

                // Create and add new visit request
                var todoItem = new TodoItem
                {
                    MrId = mrId,
                    HcoId = hcoId,
                    VisitDate = visitDate,
                    Purpose = purpose,
                    CurrentStatus = "Requested"
                };

                _unitOfWork.DbContext.Add(todoItem);

                // Create related status history (atomic with visit request)
                var statusHistory = new TodoStatusHistory
                {
                    TodoItemId = todoItem.Id,
                    Status = "Requested",
                    Timestamp = _auditContext.UtcNow,
                    ChangedBy = _auditContext.UserId,
                    Notes = "Visit request created"
                };

                _unitOfWork.DbContext.Add(statusHistory);

                // Save both entities with audit fields
                await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);

                // Commit the transaction (both entities or neither)
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

        /// <summary>
        /// Soft-deletes a visit request.
        /// Demonstrates single entity mutation with soft-delete.
        /// </summary>
        public async Task<TodoItem> DeleteTodoItemAsync(
            string todoItemId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(todoItemId))
                throw new ArgumentException("TodoItem ID cannot be empty", nameof(todoItemId));

            var transaction = await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // Direct DbContext query
                var todoItem = await _unitOfWork.DbContext.Set<TodoItem>()
                    .FirstOrDefaultAsync(v => v.Id == todoItemId, cancellationToken: ct);

                if (todoItem == null)
                    throw new BusinessLogicException($"TodoItem '{todoItemId}' not found");

                // Use domain service for soft-delete
                _softDeleteService.MarkAsDeleted(todoItem, _auditContext.UserId);

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
}
```

### Test with In-Memory DbContext

```csharp
// ForgeKit.Api.Tests/Services/TodoServiceTests.cs
using Api.Services.Visits;
using Api.Interfaces;
using Api.Data;
using Api.Entities.Visits;
using Api.Domain.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Moq;

[TestFixture]
public class TodoServiceTests
{
    private AppDbContext _dbContext;
    private IUnitOfWork _unitOfWork;
    private TodoService _service;
    private Mock<IAuditContext> _auditContextMock;

    [SetUp]
    public void SetUp()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);

        _auditContextMock = new Mock<IAuditContext>();
        _auditContextMock.Setup(x => x.UserId).Returns("test-user");
        _auditContextMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var softDeleteService = new SoftDeleteDomainService();
        _service = new TodoService(_unitOfWork, _auditContextMock.Object, softDeleteService);
    }

    [Test]
    public async Task CreateTodoItem_WithValidData_SavesToDatabase()
    {
        // Arrange
        var member = new Entities.Core.Member { Id = "mr-1", Name = "John" };
        var workspace = new Entities.Core.Workspace { Id = "hco-1", Name = "Hospital" };
        _dbContext.Add(mr);
        _dbContext.Add(hco);
        await _dbContext.SaveChangesAsync();

        var visitDate = DateTime.UtcNow.AddDays(5);

        // Act
        var result = await _service.CreateTodoAsync("mr-1", "hco-1", visitDate, "Training");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Null);

        // Verify in database using direct DbContext query
        var saved = await _dbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == result.Id);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.MrId, Is.EqualTo("mr-1"));
        Assert.That(saved.HcoId, Is.EqualTo("hco-1"));
        Assert.That(saved.VisitDate, Is.EqualTo(visitDate));
    }

    [Test]
    public async Task GetVisitsByMrId_ReturnsOnlyForSpecificMr()
    {
        // Arrange
        var mr1 = new Entities.Core.Member { Id = "mr-1", Name = "John" };
        var mr2 = new Entities.Core.Member { Id = "mr-2", Name = "Jane" };
        var workspace = new Entities.Core.Workspace { Id = "hco-1", Name = "Hospital" };

        _dbContext.AddRange(mr1, mr2, hco);

        var visit1 = new TodoItem { MrId = "mr-1", HcoId = "hco-1", VisitDate = DateTime.UtcNow.AddDays(1) };
        var visit2 = new TodoItem { MrId = "mr-1", HcoId = "hco-1", VisitDate = DateTime.UtcNow.AddDays(2) };
        var visit3 = new TodoItem { MrId = "mr-2", HcoId = "hco-1", VisitDate = DateTime.UtcNow.AddDays(3) };

        _dbContext.AddRange(visit1, visit2, visit3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetVisitsByMrIdAsync("mr-1", includeDetails: false);

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(v => v.MrId == "mr-1"), Is.True);
    }

    [Test]
    public async Task DeleteTodoItem_SoftDeletesRecord()
    {
        // Arrange
        var workspace = new Entities.Core.Workspace { Id = "hco-1", Name = "Hospital" };
        var member = new Entities.Core.Member { Id = "mr-1", Name = "John" };
        _dbContext.AddRange(workspace, mr);

        var visit = new TodoItem { MrId = "mr-1", HcoId = "hco-1", VisitDate = DateTime.UtcNow.AddDays(1) };
        _dbContext.Add(visit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteTodoItemAsync(visit.Id);

        // Assert
        Assert.That(result.IsDeleted, Is.True);
        Assert.That(result.DeletedAt, Is.Not.Null);

        // Verify record is actually marked as deleted in database
        var softDeleted = await _dbContext.Set<TodoItem>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == visit.Id);

        Assert.That(softDeleted.IsDeleted, Is.True);

        // Verify normal query excludes it (due to soft-delete filter)
        var notFound = await _dbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == visit.Id);

        Assert.That(notFound, Is.Null);
    }

    [TearDown]
    public void TearDown()
    {
        _unitOfWork?.Dispose();
        _dbContext?.Dispose();
    }
}
```

## ForgeKit-Specific Considerations

### Query Performance
When using direct DbContext:
- Use `.Select()` for projections (return only needed fields)
- Use `.Include()` to avoid N+1 queries
- Avoid loading entire entities when you only need 2-3 fields

```csharp
// Good: Projection
var visitSummaries = await _unitOfWork.DbContext.Set<TodoItem>()
    .Where(v => v.MrId == mrId)
    .Select(v => new { v.Id, v.VisitDate, v.CurrentStatus })
    .ToListAsync(ct);

// Less Good: Load entire entity
var visits = await _unitOfWork.DbContext.Set<TodoItem>()
    .Where(v => v.MrId == mrId)
    .ToListAsync(ct);
```

### Soft-Delete Filter Usage
Remember global soft-delete filters exclude deleted records:

```csharp
// Won't see soft-deleted records (filtered by global filter)
var activeVisits = await _unitOfWork.DbContext.Set<TodoItem>()
    .ToListAsync(ct);

// Will see soft-deleted records (filter disabled)
var allVisits = await _unitOfWork.DbContext.Set<TodoItem>()
    .IgnoreQueryFilters()
    .ToListAsync(ct);
```

## Related ADRs

- **ADR-001:** Module pattern (services organized in modules)
- **ADR-002:** Soft-delete pattern (queries use global filter)
- **ADR-007:** Unit of Work pattern (manages transactions)

## References

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Repository Pattern is Considered Harmful - Jimmy Bogard](https://jimmybogard.com/repository-pattern-considered-harmful/)
- [Why Repository Pattern is Obsolete with LINQ](https://github.com/dotnet/efcore/issues/5833)
