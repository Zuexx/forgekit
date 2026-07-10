# Soft-Delete Archiving Capability Specification

**Status:** Specification  
**Created:** 2026-02-09  
**Related Design:** ../../design.md

---

## ADDED Requirements

### Requirement: SoftDeleteDomainService Class
The system SHALL provide a SoftDeleteDomainService that manages soft-deletion and restoration of entities.

**Acceptance Criteria:**
- Class MUST be located in Api/Domain/Services/SoftDeleteDomainService.cs
- Class MUST provide generic `MarkAsDeleted<T>()` method
- Class MUST provide generic `Restore<T>()` method
- Class MUST provide generic `CanRestore<T>()` method
- Grace period MUST be configurable (default: 30 days)
- All methods MUST work with any entity inheriting from BaseEntity

#### Scenario: Mark entity as deleted
- **WHEN** an application service calls MarkAsDeleted(todoItem, "user-123")
- **THEN** entity.IsDeleted is set to true
- **THEN** entity.DeletedAt is set to current UTC timestamp
- **THEN** entity.DeletedBy is set to "user-123"

#### Scenario: Check if entity can be restored
- **WHEN** an entity was deleted 15 days ago
- **AND** CanRestore(entity) is called
- **THEN** it SHALL return true (within 30-day grace period)

#### Scenario: Prevent restoration after grace period
- **WHEN** an entity was deleted 35 days ago
- **AND** CanRestore(entity) is called
- **THEN** it SHALL return false (beyond 30-day grace period)

#### Scenario: Restore entity to active state
- **WHEN** Restore(entity, "user-456") is called on a restorable entity
- **THEN** entity.IsDeleted is set to false
- **THEN** entity.DeletedAt is set to null
- **THEN** entity.DeletedBy is set to null
- **THEN** entity.UpdatedBy is set to "user-456"
- **THEN** entity.UpdatedAt is set to current UTC timestamp

#### Scenario: Prevent restoration of ineligible entities
- **WHEN** Restore(entity, "user-456") is called on a non-restorable entity
- **THEN** an InvalidOperationException SHALL be thrown
- **THEN** entity state SHALL NOT be modified

---

### Requirement: Query Filter Exclusion
The system SHALL automatically exclude soft-deleted records from all queries unless explicitly overridden.

**Acceptance Criteria:**
- All deletable entities MUST have global query filters configured
- Query filter MUST be: `HasQueryFilter(e => !e.IsDeleted)`
- Filter MUST be defined in AppDbContext.OnModelCreating()
- Filter MUST apply to all queries unless IgnoreQueryFilters() is used
- No existing soft-delete behavior SHALL be changed

#### Scenario: Normal queries exclude soft-deleted records
- **WHEN** MarkAsDeleted(todoItem, "user-123") is called
- **AND** await dbContext.SaveChangesAsync()
- **AND** var found = await dbContext.TodoItems.FirstOrDefaultAsync(v => v.Id == id)
- **THEN** found SHALL be null (record excluded by filter)

#### Scenario: IgnoreQueryFilters includes soft-deleted records
- **WHEN** a deleted entity exists in database
- **AND** var found = await dbContext.TodoItems.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Id == id)
- **THEN** found SHALL return the deleted entity
- **THEN** found.IsDeleted SHALL be true

---

### Requirement: Sample Todo Service Restore Integration
The system SHALL provide sample application-level restore functionality that validates restore eligibility and maintains audit trails.

**Acceptance Criteria:**
- RestoreTodoAsync() method MUST exist in the sample TodoService
- Method MUST use IgnoreQueryFilters() to retrieve deleted entity
- Method MUST validate restore eligibility before restoration
- Method MUST throw InvalidOperationException if not eligible
- Method MUST use SoftDeleteDomainService.Restore()
- Method MUST update audit fields correctly
- Method MUST persist changes via UnitOfWork

#### Scenario: Restore deleted sample todo
- **WHEN** RestoreTodoAsync("todo-123") is called
- **AND** entity was deleted within 30 days
- **AND** entity is loaded with IgnoreQueryFilters()
- **THEN** SoftDeleteDomainService.Restore() is called
- **THEN** entity.IsDeleted becomes false
- **THEN** entity.UpdatedBy is set to current user ID
- **THEN** changes are persisted to database

#### Scenario: Prevent restoration beyond grace period
- **WHEN** RestoreTodoAsync("todo-123") is called
- **AND** entity was deleted 35 days ago
- **THEN** CanRestore() returns false
- **THEN** InvalidOperationException is thrown with message about grace period
- **THEN** entity state is NOT modified

#### Scenario: Audit trail preserved through deletion and restoration
- **WHEN** entity is created at timestamp T1 by user "creator"
- **AND** entity is modified at timestamp T2 by user "modifier"
- **AND** entity is deleted at timestamp T3 by user "deleter"
- **AND** entity is restored at timestamp T4 by user "restorer"
- **THEN** CreatedBy remains "creator" (unchanged)
- **THEN** CreatedAt remains T1 (unchanged)
- **THEN** UpdatedBy is "restorer"
- **THEN** UpdatedAt is T4

---

### Requirement: DI Registration
The system SHALL register SoftDeleteDomainService in the dependency injection container.

**Acceptance Criteria:**
- SoftDeleteDomainService MUST be registered as scoped service
- Registration MUST occur in Program.cs
- Service MUST be injectable into application services
- Service MUST have access to domain layer utilities if needed

#### Scenario: Application Service receives SoftDeleteDomainService via DI
- **WHEN** Program.cs registers `builder.Services.AddScoped<SoftDeleteDomainService>()`
- **AND** TodoService constructor requests SoftDeleteDomainService
- **THEN** an instance is injected successfully
- **THEN** MarkAsDeleted() and Restore() methods are available

---

### Requirement: Sample Todo Service Deletion Operations
Sample application services SHALL use SoftDeleteDomainService for deletion operations instead of direct entity modification.

**Acceptance Criteria:**
- TodoService.DeleteTodoAsync() MUST use SoftDeleteDomainService.MarkAsDeleted()
- Direct manipulation of IsDeleted/DeletedAt/DeletedBy MUST NOT occur in application layer
- All deletions MUST record user ID from IAuditContext
- Deletion MUST be persisted through UnitOfWork.SaveChangesAsync()

#### Scenario: Delete sample todo through service
- **WHEN** DeleteTodoAsync(todoId) is called
- **AND** entity is loaded from database
- **THEN** _softDeleteService.MarkAsDeleted(entity, _auditContext.UserId) is called
- **THEN** _unitOfWork.SaveChangesAsync() persists the change
- **THEN** entity becomes invisible in subsequent queries

---

## Compliance & Testing

### Testing Requirements
- Unit tests for SoftDeleteDomainService (MarkAsDeleted, Restore, CanRestore)
- Time-based tests for grace period validation (mocked DateTime)
- Integration tests for query filter behavior
- Integration tests for IgnoreQueryFilters()
- Audit trail preservation tests through full delete/restore cycle

### Success Metrics
- All 126 existing tests continue to pass
- Minimum 12 new tests for soft-delete archiving (unit + integration)
- 100% code coverage for SoftDeleteDomainService
- No regression in query performance

---

## Related Specifications

- **Audit Context** (../audit-context/spec.md): Provides user identity for audit fields
- **Unit of Work** (../../implement-unit-of-work/spec.md): Manages transaction persistence
- **Base Entity** (../../add-efcore-entities/spec.md): Provides IsDeleted/DeletedAt/DeletedBy fields
