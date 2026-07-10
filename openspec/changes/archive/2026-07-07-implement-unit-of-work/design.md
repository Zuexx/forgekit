# Design: Unit of Work Pattern Implementation

**Status:** Design  
**Created:** 2026-02-04  
**Related Proposal:** implement-unit-of-work/proposal.md  

---

## Overview

This document provides the technical design for implementing the Unit of Work pattern across the Scaffold API codebase.

---

## Architecture Design

### Component Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (Modules)                       │
├─────────────────────────────────────────────────────────────┤
│  VisitsModule → Handler → TodoService            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              Application Service Layer                       │
├─────────────────────────────────────────────────────────────┤
│  TodoService                                     │
│    └─ Uses IUnitOfWork for transaction boundaries           │
│    └─ Uses VisitDomainService for business rules            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              Data Access Layer (Unit of Work)               │
├─────────────────────────────────────────────────────────────┤
│  IUnitOfWork                                                │
│    ├─ DbContext property (direct access)                    │
│    ├─ BeginTransactionAsync()                               │
│    ├─ CommitTransactionAsync()                              │
│    ├─ RollbackTransactionAsync()                            │
│    └─ SaveChangesAsync(userId)                              │
│                                                              │
│  UnitOfWork : IUnitOfWork                                   │
│    ├─ Manages IDbContextTransaction                         │
│    ├─ Provides DbContext access                             │
│    └─ Sets audit fields automatically                       │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│           EF Core / Database Layer                          │
├─────────────────────────────────────────────────────────────┤
│  AppDbContext (unchanged)                                  │
│  SQL Server Database                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Interface Design: IUnitOfWork

### Location
`Api/Interfaces/IUnitOfWork.cs`

### Properties
```csharp
AppDbContext DbContext { get; }
```
- Direct access to DbContext
- Application Service uses this for both queries and entity modifications
- No abstraction or Repository pattern

### Methods

#### BeginTransactionAsync()
```csharp
Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
```
- Starts database transaction
- Returns IDbContextTransaction for manual transaction control (if needed)
- All subsequent operations are atomic until CommitTransactionAsync() or RollbackTransactionAsync()

#### CommitTransactionAsync()
```csharp
Task CommitTransactionAsync(CancellationToken ct = default);
```
- Saves all changes to database
- Commits transaction
- Automatic rollback if SaveChangesAsync() fails
- Disposes transaction after completion

#### RollbackTransactionAsync()
```csharp
Task RollbackTransactionAsync(CancellationToken ct = default);
```
- Reverts all changes made since BeginTransactionAsync()
- Used in catch blocks or explicit rollback scenarios
- Disposes transaction after rollback

#### SaveChangesAsync()
```csharp
Task<int> SaveChangesAsync(CancellationToken ct = default);
Task<int> SaveChangesAsync(string userId, CancellationToken ct = default);
```
- First overload: saves without audit field management
- Second overload: saves AND populates CreatedBy/UpdatedBy fields

---

## Implementation Design: UnitOfWork

### Location
`Api/Data/UnitOfWork.cs`

### Constructor
```csharp
public UnitOfWork(AppDbContext dbContext)
```
- Injected via DI container
- Takes AppDbContext as dependency

### Private Fields
```csharp
private readonly AppDbContext _dbContext;
private IDbContextTransaction? _transaction;
```

### Key Methods

#### BeginTransactionAsync()
```csharp
public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
{
    _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
    return _transaction;
}
```
- Calls EF Core's Database.BeginTransactionAsync()
- Stores transaction reference for later commit/rollback

#### CommitTransactionAsync()
```csharp
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
```
- Try/catch/finally ensures cleanup
- Automatic rollback on exception
- Always disposes transaction

#### SetAuditFields()
```csharp
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
```
- Uses ChangeTracker to find modified entities
- Only processes entities implementing IAuditableEntity
- Sets CreatedBy only for new entities
- Always sets UpdatedBy for both new and modified

---

## DI Registration

### Location
`Program.cs`

### Registration Code
```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```
- Scoped lifetime: one instance per HTTP request
- Ensures clean transaction boundaries per request
- Automatic disposal of DbContext when scope ends

---

## Usage Pattern

### In Application Service

```csharp
public async Task<TodoItem> CreateTodoItemAsync(
    string mrId,
    string hcoId,
    CancellationToken ct)
{
    try
    {
        // 1. START TRANSACTION
        await _unitOfWork.BeginTransactionAsync(ct);

        // 2. LOAD & VALIDATE
        var member = await _unitOfWork.DbContext.Members
            .FirstOrDefaultAsync(m => m.Id == mrId && !m.IsDeleted, ct);
        if (member == null) throw new NotFoundException(...);

        var workspace = await _unitOfWork.DbContext.Workspaces
            .FirstOrDefaultAsync(h => h.Id == hcoId && !h.IsDeleted, ct);
        if (workspace == null) throw new NotFoundException(...);

        // 3. APPLY BUSINESS RULES
        if (!_domainService.IsRepresentativeAuthorizedForOrg(member, hco))
            throw new UnauthorizedException(...);

        // 4. CREATE ENTITIES (not yet committed)
        var todoItem = new TodoItem { MrId = mrId, HcoId = hcoId, ... };
        _unitOfWork.DbContext.TodoItems.Add(todoItem);

        var statusHistory = new TodoStatusHistory { TodoItemId = todoItem.Id, ... };
        _unitOfWork.DbContext.TodoStatusHistory.Add(statusHistory);

        // 5. COMMIT ATOMICALLY
        await _unitOfWork.CommitTransactionAsync(ct);

        return todoItem;
    }
    catch (Exception ex)
    {
        // 6. AUTOMATIC ROLLBACK
        await _unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
}
```

### Transaction Flow

```
BEGIN TRANSACTION
  ├─ Validate MR exists
  ├─ Validate HCO exists
  ├─ Check authorization
  ├─ Create TodoItem (not committed)
  ├─ Create TodoStatusHistory (not committed)
  └─ COMMIT or ROLLBACK (atomic operation)
```

---

## Error Handling Strategy

### Exception Types

| Exception | When | Handler | Result |
|-----------|------|---------|--------|
| NotFoundException | Entity not found during validation | Throw immediately | Rollback entire transaction |
| UnauthorizedException | Business rule violation | Throw immediately | Rollback entire transaction |
| DbUpdateException | SaveChangesAsync fails | Catch, rollback, rethrow | Rollback entire transaction |
| OperationCanceledException | CancellationToken cancellation | Catch, rollback, rethrow | Rollback entire transaction |

### Rollback Behavior
- Any exception during transaction triggers automatic rollback
- All entities added/modified are reverted
- Exception is re-thrown after cleanup

---

## Data Consistency Guarantees

### ACID Properties

**Atomicity:** ✅
- All changes succeed or all fail
- BeginTransaction/CommitTransaction/RollbackTransaction provides all-or-nothing semantics

**Consistency:** ✅
- Domain Service enforces business rules before SaveChanges
- Audit fields always populated
- Database constraints enforced by EF Core

**Isolation:** ✅
- Database transaction handles isolation
- Concurrent requests have separate UnitOfWork instances

**Durability:** ✅
- CommitTransactionAsync() ensures disk writes
- SQL Server handles durability guarantees

---

## Testing Strategy

### Unit Tests
- Test transaction commit with multiple entities
- Test transaction rollback discards all changes
- Test audit field population
- Use InMemoryDatabase for isolation

### Integration Tests
- Test real workflow (TodoService)
- Test with real database (if possible)
- Verify atomicity with intentional failures

---

## Performance Considerations

### Transaction Overhead
- Minimal: database-level transactions are efficient
- Single round trip to database per transaction
- No additional queries

### Memory Usage
- Entities tracked by DbContext during transaction
- Typical scenario: < 100 entities tracked
- No memory concerns for normal use cases

### Concurrency
- Scoped lifetime ensures no shared transaction state
- Each request has isolated transaction
- Database handles concurrent transaction isolation

---

## Migration Path

### Phase 1: Foundation (This task)
- Create IUnitOfWork interface
- Create UnitOfWork implementation
- Register in DI

### Phase 2: Initial Usage
- Update TodoService to use UnitOfWork
- Write unit tests

### Phase 3: Adoption
- Update other Application Services as they're created
- Establish pattern for new features

---

## Design Decisions Rationale

### Decision: Expose DbContext Directly
**Rationale:**
- Unit of Work is purely a transaction boundary pattern
- Avoids Repository pattern (which we've rejected)
- Application Service needs direct DbContext access for queries anyway
- No abstraction benefit from hiding DbContext

### Decision: Scoped Lifetime
**Rationale:**
- One instance per HTTP request
- Clean boundaries for transaction scope
- Automatic cleanup when scope ends
- Matches natural request boundaries

### Decision: Separate Audit Field Logic
**Rationale:**
- Centralizes CreatedBy/UpdatedBy handling
- No logic scattered across services
- Easy to test and maintain
- Single responsibility

---

## Compatibility Notes

### Backward Compatibility
- No breaking changes to existing code
- Optional use: Application Services can adopt gradually
- DbContext still works without UnitOfWork for simple operations

### EF Core Compatibility
- Compatible with EF Core 7.0+ (current version)
- Uses standard Database.BeginTransactionAsync()
- No special EF Core features required

---

## Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-02-04 | Architecture Team | Initial design document |
