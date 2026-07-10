# ADR-002: Soft-Delete with Grace Period Restore

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-004, ADR-005, ADR-007

## Context

Real-world applications frequently require "undo" functionality for deleted data. Permanent deletion poses risks:
- **Accidental Deletion:** Users cannot recover mistakenly deleted records
- **Audit Requirements:** Compliance may require retention of deletion history
- **Business Logic:** Features like "restore deleted visit requests" require deletion history
- **Data Recovery:** Database backups might be necessary to recover permanently deleted data

However, unlimited deletion recovery creates other problems:
- **Unbounded Storage:** Deleted records accumulate indefinitely
- **Query Performance:** Soft-delete filters slow down queries
- **Data Privacy:** GDPR/CCPA require actual deletion after certain periods
- **Compliance Cleanup:** Regulations demand permanent data purging

## Decision

Implement **Soft-Delete with Grace Period Restore** where:

1. **Soft Delete (Mark as Deleted):**
   - Entity marked as deleted with `IsDeleted = true`, `DeletedAt`, `DeletedBy`
   - Record remains in database but excluded from normal queries via global query filter
   - Full deletion history preserved for audit trail

2. **Grace Period (Restore Window):**
   - Soft-deleted records can be restored within **30 days** of deletion
   - After 30 days, records can only be permanently deleted
   - Grace period is configured constant (can be made configurable)

3. **Domain Service Management:**
   - `SoftDeleteDomainService` handles all soft-delete/restore operations
   - Ensures consistency and business rule enforcement
   - Single source of truth for deletion logic

4. **Query Filters:**
   - Global query filter excludes soft-deleted records from normal queries
   - `IgnoreQueryFilters()` available when explicitly loading deleted records
   - Prevents accidental inclusion of deleted data in results

5. **Audit Trail:**
   - All deletions and restorations tracked with user ID and timestamp
   - Full recovery path available for investigation

## Rationale

### Why Soft-Delete?

1. **Data Preservation:** Accidentally deleted data is recoverable without full system restore
2. **Audit Requirements:** Full history of creation, modification, and deletion for compliance
3. **Business Intelligence:** Historical data available for analytics (can see deleted trends)
4. **Undo Functionality:** Users can restore recently deleted records in UI
5. **Non-Destructive:** No data loss even if business rules change later

### Why Grace Period (Not Unlimited)?

1. **Data Privacy Compliance:**
   - GDPR "right to be forgotten" requires permanent deletion timelines
   - CCPA requires deletion of personal data within specified timeframe
   - Grace period is reasonable accommodation between usability and compliance

2. **Storage Efficiency:**
   - Prevents indefinite accumulation of deleted records
   - Soft-delete doesn't save storage if records are never cleaned up
   - 30-day window balances recovery and storage concerns

3. **Query Performance:**
   - Soft-deleted records older than grace period can be archived/hard-deleted
   - Query filters operate on smaller datasets
   - Retention policies can be implemented

4. **Operational Clarity:**
   - Clear boundary: "Can restore" vs "Requires database admin"
   - Users understand deletion is temporary, not permanent for 30 days
   - After 30 days, deletion is effectively permanent (from user perspective)

### Why Domain Service?

1. **Single Responsibility:** All deletion logic in one place
2. **Consistency Enforcement:** Business rules applied uniformly
   - Can't forget to set DeletedAt
   - Can't restore beyond grace period without explicit override
   - Can't restore without valid user context

3. **Testability:** Deletion logic can be unit tested independently
4. **Reusability:** MarkAsDeleted and Restore can be used across any entity type
5. **Evolution:** Business rules can change in one place
   - Currently: 30-day restore window
   - Future: Could add "soft-delete reason" field
   - Future: Could add permission checks for restoration

## Alternatives Considered

### 1. Hard Delete (No Soft Delete)
**Approach:** Permanently delete records from database immediately

**Pros:**
- Simplest implementation (just DELETE)
- No query filters needed
- Storage efficient
- Faster queries
- Clear data lifecycle

**Cons:**
- No undo capability
- Data loss if business rules change
- Audit trail incomplete (deletion not recorded)
- Doesn't meet compliance retention requirements
- Requires database backups for recovery
- Developers accidentally expose deleted data in backups

**When Better:** Logging tables, audit logs (where deletion is implicit and intended)

---

### 2. Unlimited Soft Delete (No Grace Period)
**Approach:** Soft-delete everything, never hard-delete, restore anytime

```csharp
public bool CanRestore<T>(T entity) where T : BaseEntity
{
    return entity.IsDeleted; // Always restorable
}
```

**Pros:**
- Maximum recoverability
- Never lose data
- Users always have undo capability
- Simplest domain service logic

**Cons:**
- Indefinite storage growth
- Violates data privacy regulations (GDPR, CCPA)
- Query filters always active (small performance penalty)
- Deleted records accumulate forever
- Unclear retention policies
- Compliance violations in regulated industries

**When Better:** Internal-only systems with strict data governance; personal data systems where deletion is not required

---

### 3. Hard Delete After Grace Period (Automatic)
**Approach:** Soft-delete immediately, automatically hard-delete after 30 days

```csharp
// Scheduled job
var expiredDeletes = dbContext.Set<Entity>()
    .IgnoreQueryFilters()
    .Where(e => e.IsDeleted && e.DeletedAt < DateTime.UtcNow.AddDays(-30))
    .ToList();

foreach (var entity in expiredDeletes)
{
    dbContext.Remove(entity); // Hard delete
}
```

**Pros:**
- Fully compliant with GDPR/CCPA
- Storage eventually released
- Clear retention timeline
- No manual cleanup needed

**Cons:**
- Requires background job infrastructure
- Data permanently lost if grace period expires
- Harder to recover if deletion was truly accidental
- Job failures could leave orphaned records
- Harder to verify grace period expiration in tests

**Trade-off:** Automatic vs manual, immediate compliance vs user recoverability

**When Better:** Strict GDPR/CCPA compliance required; high-volume deletions; no recovery needed after grace period

---

### 4. Archive Pattern (Hard Delete → Archive Separately)
**Approach:** Soft-delete to archive table, then hard-delete after grace period

```csharp
public async Task<int> ArchiveExpiredDeletedRecords()
{
    var expired = dbContext.Set<Entity>()
        .IgnoreQueryFilters()
        .Where(e => e.IsDeleted && e.DeletedAt < DateTime.UtcNow.AddDays(-30))
        .ToList();

    var archivedEntities = expired.Select(e => new ArchivedEntity {
        OriginalId = e.Id,
        DeletedAt = e.DeletedAt,
        Data = JsonConvert.SerializeObject(e)
    });

    dbContext.Set<ArchivedEntity>().AddRange(archivedEntities);
    dbContext.Set<Entity>().RemoveRange(expired);

    return await dbContext.SaveChangesAsync();
}
```

**Pros:**
- Maximum compliance
- Audit trail preserved (JSON snapshot)
- Live database smaller (better query performance)
- Archive table can be in separate database/storage
- Can analyze historical trends from archive

**Cons:**
- Higher implementation complexity
- Requires archive table schema
- Data restoration from archive is manual/involved
- Archive management adds operational burden
- Need separate backup/retention policy for archive

**Trade-off:** Compliance and performance vs complexity

**When Better:** Large multi-tenant systems; historical trend analysis important; audit requirements very strict

---

### 5. Multiple Grace Periods
**Approach:** Different retention periods for different entity types

```csharp
public bool CanRestore<T>(T entity, int? graceDays = null) where T : BaseEntity
{
    var days = graceDays ?? GetDefaultGracePeriod(typeof(T));
    return entity.IsDeleted &&
           entity.DeletedAt.HasValue &&
           (DateTime.UtcNow - entity.DeletedAt.Value).TotalDays <= days;
}

// TodoItem: 30 days
// Audit: 90 days
// Logs: 7 days
```

**Pros:**
- Tailored retention per entity type
- Sensitive data deleted faster (7-day log retention)
- Less critical data retained longer (90-day audit)
- Can match real business needs precisely

**Cons:**
- More complex configuration
- Developers must remember different periods
- Harder to implement automatic cleanup (per-table background jobs)
- Confusing for users (why can't I restore this but can that?)
- Migration burden if periods change

**Trade-off:** Flexibility vs simplicity

**When Better:** Complex systems with very different retention requirements per entity; highly regulated scenarios

---

## Consequences

### Positive

1. **Accidental Deletion Recovery:** Users can undo mistakes for 30 days
2. **Full Audit Trail:** Complete history of when records were created, modified, deleted
3. **Compliance Ready:** 30-day grace period aligns with GDPR "right to be forgotten"
4. **Query Performance:** Soft-deleted records excluded by default (no performance penalty)
5. **Operational Safety:** No data loss without explicit hard-delete action
6. **User Experience:** "Restore" button in UI improves user satisfaction

### Negative

1. **Query Filter Requirement:** Every query filter must exclude soft-deleted records (implicit, can be forgotten)
2. **Grace Period Limitation:** After 30 days, recovery requires admin intervention or database access
3. **Storage Overhead:** Deleted records consume storage until purged (manageable with cleanup jobs)
4. **Testing Complexity:** Tests must account for soft-delete filters and IgnoreQueryFilters() usage
5. **Index Bloat:** Indexes include soft-deleted records (minor performance impact on large datasets)

### Neutral

1. **Architectural Pattern:** Adds complexity vs hard-delete, but standard across industry
2. **Migration Path:** Existing hard-delete data incompatible with soft-delete (one-way migration)
3. **User Expectations:** Users must be educated about grace period (30 days, not permanent recovery)

## When to Use

✅ **Use Soft-Delete with Grace Period when:**
- Accidental deletion is high-risk (financial, compliance, customer satisfaction)
- Regulatory compliance required (GDPR, CCPA, HIPAA, SOX)
- Users need undo capability in UI
- Deletion should be auditable
- Business logic depends on deletion history (analytics, reporting)
- Team wants to avoid data loss scenarios
- Application is in regulated industry (healthcare, finance)

✅ **Specifically for:**
- Core business entities (TodoItem, Account, Patient, etc.)
- User-facing operations (user creates/deletes in UI)
- Compliance-sensitive data (audit logs, transactions)

## When NOT to Use

❌ **Avoid Soft-Delete when:**
- Data is purely transient (cache, session data)
- Deletion should be truly permanent (user deletion under GDPR, system cleanup)
- Performance critical and soft-delete filters impact query speed significantly
- Storage is severely constrained
- Compliance explicitly requires immediate permanent deletion
- Data is not auditable or tracked (logs, metrics)

❌ **Don't use for:**
- Temporary working tables
- Cache tables (delete instead of soft-delete)
- Audit logs themselves (already immutable)
- Session data

## ForgeKit Implementation

### Entity Base Class

```csharp
// ForgeKit.Api/Entities/Base/ISoftDelete.cs
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

// ForgeKit.Api/Entities/Base/BaseEntity.cs
public abstract class BaseEntity : IAuditableEntity, ISoftDelete
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; }

    // Soft-delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### Domain Service

```csharp
// ForgeKit.Api/Domain/Services/SoftDeleteDomainService.cs
public class SoftDeleteDomainService
{
    private const int DefaultRestoreDaysLimit = 30;

    /// <summary>
    /// Marks an entity as soft-deleted with audit information.
    /// </summary>
    public void MarkAsDeleted<T>(T entity, string deletedBy, DateTime? deletedAt = null)
        where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        entity.IsDeleted = true;
        entity.DeletedAt = deletedAt ?? DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity back to active state.
    /// </summary>
    public void Restore<T>(T entity, string restoredBy) where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(restoredBy))
            throw new ArgumentException("RestoredBy cannot be null or empty", nameof(restoredBy));

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.UpdatedBy = restoredBy;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if an entity can be restored based on grace period.
    /// Returns true only if deleted within the last 30 days.
    /// </summary>
    public bool CanRestore<T>(T entity, int restoreDaysLimit = DefaultRestoreDaysLimit)
        where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return entity.IsDeleted &&
               entity.DeletedAt.HasValue &&
               (DateTime.UtcNow - entity.DeletedAt.Value).TotalDays <= restoreDaysLimit;
    }
}
```

### Query Filter in DbContext

```csharp
// ForgeKit.Api/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    // ... other configuration

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply soft-delete filter to all entities
        // This automatically excludes soft-deleted records from queries
        var softDeleteMethod = typeof(EF.Functions).GetMethod(
            nameof(EF.Functions.Like),
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(string) },
            null)!
            .GetGenericMethodDefinition();

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entity.ClrType))
            {
                modelBuilder.Entity(entity.ClrType)
                    .HasQueryFilter(
                        EF.Property<bool>(entity.ClrType, "IsDeleted") == false);
            }
        }
    }
}
```

### TodoService Usage

```csharp
// ForgeKit.Api/Services/Visits/TodoService.cs
public class TodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditContext _auditContext;
    private readonly SoftDeleteDomainService _softDeleteService;

    public async Task<TodoItem> DeleteTodoItemAsync(
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

            // Use domain service for consistent soft-delete
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

    public async Task<TodoItem> RestoreTodoItemAsync(
        string todoItemId,
        CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Must use IgnoreQueryFilters() to load soft-deleted records
            var todoItem = await _unitOfWork.DbContext.Set<TodoItem>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == todoItemId && v.IsDeleted, ct);

            if (todoItem == null)
                throw new BusinessLogicException($"Soft-deleted TodoItem '{todoItemId}' not found");

            // Check grace period business rule
            if (!_softDeleteService.CanRestore(todoItem))
                throw new BusinessLogicException(
                    "Cannot restore: soft-deleted records can only be restored within 30 days");

            // Use domain service for consistent restoration
            _softDeleteService.Restore(todoItem, _auditContext.UserId);

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

### Test Examples

```csharp
// ForgeKit.Api.Tests/Domain/Services/SoftDeleteDomainServiceTests.cs
[TestFixture]
public class SoftDeleteDomainServiceTests
{
    private SoftDeleteDomainService _service;

    [SetUp]
    public void SetUp() => _service = new SoftDeleteDomainService();

    [Test]
    public void MarkAsDeleted_SetsIsDeletedTrue()
    {
        var entity = new TodoItem { Id = "123" };
        var userId = "user-123";

        _service.MarkAsDeleted(entity, userId);

        Assert.That(entity.IsDeleted, Is.True);
        Assert.That(entity.DeletedBy, Is.EqualTo(userId));
        Assert.That(entity.DeletedAt, Is.Not.Null);
    }

    [Test]
    public void CanRestore_WithinGracePeriod_ReturnsTrue()
    {
        var entity = new TodoItem {
            Id = "123",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-15)
        };

        var canRestore = _service.CanRestore(entity);

        Assert.That(canRestore, Is.True);
    }

    [Test]
    public void CanRestore_AfterGracePeriod_ReturnsFalse()
    {
        var entity = new TodoItem {
            Id = "123",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-31)
        };

        var canRestore = _service.CanRestore(entity);

        Assert.That(canRestore, Is.False);
    }

    [Test]
    public void Restore_ClearsDeletedFields()
    {
        var entity = new TodoItem {
            Id = "123",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-5),
            DeletedBy = "deleter"
        };
        var restorerId = "restorer-123";

        _service.Restore(entity, restorerId);

        Assert.That(entity.IsDeleted, Is.False);
        Assert.That(entity.DeletedAt, Is.Null);
        Assert.That(entity.DeletedBy, Is.Null);
        Assert.That(entity.UpdatedBy, Is.EqualTo(restorerId));
    }
}
```

## Operational Considerations

### Manual Hard Delete (After Grace Period)

```csharp
// For compliance cleanup after grace period expires
public async Task<int> PermanentlyDeleteExpiredSoftDeletesAsync(
    int daysToRetain = 30,
    CancellationToken ct = default)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysToRetain);

    var expiredRecords = await _dbContext.Set<TodoItem>()
        .IgnoreQueryFilters()
        .Where(e => e.IsDeleted && e.DeletedAt <= cutoffDate)
        .ToListAsync(ct);

    _dbContext.RemoveRange(expiredRecords);
    return await _dbContext.SaveChangesAsync(ct);
}
```

## Related ADRs

- **ADR-004:** Direct DbContext usage in services (soft-delete queries use DbContext)
- **ADR-005:** Audit context service (provides user ID for DeletedBy)
- **ADR-007:** Unit of Work pattern (manages transactions for delete/restore operations)

## References

- [Microsoft: Soft Delete Pattern](https://docs.microsoft.com/en-us/ef/core/modeling/entity-properties?tabs=data-annotations)
- [GDPR Data Deletion Requirements](https://gdpr-info.eu/article-5/)
- [CCPA Right to Deletion](https://oag.ca.gov/privacy/ccpa)
