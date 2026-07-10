# Unit of Work Pattern Specification

**Status:** Specification  
**Created:** 2026-02-04  
**Related Design:** ../../design.md  

---

## ADDED Requirements

### Requirement: IUnitOfWork Interface
The system SHALL provide an IUnitOfWork interface for managing atomic transactions across multiple entity saves.

**Acceptance Criteria:**
- Interface MUST inherit from IDisposable
- Interface MUST be located in Api/Interfaces/IUnitOfWork.cs
- Interface MUST expose DbContext property for direct access
- Interface MUST provide BeginTransactionAsync, CommitTransactionAsync, and RollbackTransactionAsync methods

#### Scenario: Developer injects IUnitOfWork to manage transactions
```
Given a developer needs to save multiple entities atomically
When they inject IUnitOfWork into Application Service
Then they SHALL call BeginTransactionAsync() to start transaction
And add multiple entities to DbContext
And call CommitTransactionAsync() to save all atomically
And any exception SHALL trigger automatic rollback
```

#### Scenario: SaveChangesAsync with userId populates audit fields
```
Given an entity implementing IAuditableEntity is added
When SaveChangesAsync("user123") is called
Then the entity.CreatedBy MUST be set to "user123"
And the entity.UpdatedBy MUST be set to "user123"
```

---

### Requirement: UnitOfWork Class Implementation
The system SHALL implement UnitOfWork class that provides transaction management with automatic audit field handling.

**Acceptance Criteria:**
- Class MUST implement IUnitOfWork
- Class MUST be located in Api/Data/UnitOfWork.cs
- Class MUST inject AppDbContext via constructor
- Class MUST manage IDbContextTransaction internally
- Class MUST handle exceptions with automatic rollback

#### Scenario: Successful atomic commit saves all entities
```
Given a transaction has been started with BeginTransactionAsync()
When multiple entities are added to DbContext
And CommitTransactionAsync() is called
Then all entities MUST be persisted to database
And the transaction MUST be committed
And the transaction MUST be disposed
```

#### Scenario: Exception during commit triggers rollback
```
Given a transaction has been started
When entities are added to DbContext
And CommitTransactionAsync() encounters a SaveChangesAsync failure
Then the transaction MUST be rolled back
And no entities SHALL be persisted
And the exception MUST be re-thrown after cleanup
```

---

### Requirement: Unit of Work Service Registration
The system SHALL register UnitOfWork in the dependency injection container.

**Acceptance Criteria:**
- IUnitOfWork MUST be registered as a scoped service
- Implementation MUST be UnitOfWork
- Registration MUST occur in Program.cs
- Registration MUST be in Data layer configuration section

#### Scenario: Application Service receives IUnitOfWork via DI
```
Given Program.cs has registered IUnitOfWork
When Application Service constructor requests IUnitOfWork
Then an instance of UnitOfWork MUST be injected
And it MUST have access to configured AppDbContext
```

#### Scenario: Each HTTP request gets fresh UnitOfWork instance
```
Given two HTTP requests are processed concurrently
When both request IUnitOfWork
Then each SHALL receive a separate UnitOfWork instance
And transactions SHALL not interfere with each other
```

---

## ADDED Requirements

### Requirement: Centralized Audit Field Population
The system SHALL ensure CreatedBy and UpdatedBy fields are consistently populated through UnitOfWork.

**Acceptance Criteria:**
- Only IAuditableEntity types MUST be processed
- CreatedBy MUST be set only on new entities (EntityState.Added)
- UpdatedBy MUST be set on both new and modified entities
- No manual setting of audit fields SHALL be needed in services

#### Scenario: New entity gets both CreatedBy and UpdatedBy
```
Given an entity is added to DbContext
When SaveChangesAsync("user123") is called
Then the entity.CreatedBy MUST equal "user123"
And the entity.UpdatedBy MUST equal "user123"
```

#### Scenario: Modified entity updates only UpdatedBy
```
Given an entity exists with CreatedBy = "user1"
When the entity is modified
And SaveChangesAsync("user2") is called
Then the entity.CreatedBy MUST remain "user1"
And the entity.UpdatedBy MUST change to "user2"
```

#### Scenario: Non-auditable entities are ignored
```
Given an entity NOT implementing IAuditableEntity
When SaveChangesAsync("user123") is called
Then the entity MUST be saved
And no audit fields SHALL be set on it
```

---

## Application Service Integration

### Requirement: Multi-Entity Atomic Operations
The system SHALL enable Application Services to save multiple entities atomically within a transaction.

**Acceptance Criteria:**
- Application Services MAY use UnitOfWork for transactions
- All changes within transaction boundary MUST be atomic
- Exception handling MUST automatically rollback
- Usage pattern MUST be clear and consistent

#### Scenario: Create TodoItem with StatusHistory atomically
```
Given TodoService.CreateTodoItemAsync is called
When BeginTransactionAsync() starts the transaction
And TodoItem is added to DbContext
And TodoStatusHistory is added to DbContext
And CommitTransactionAsync() saves both
Then both entities MUST be persisted together
Or if authorization check fails, neither SHALL be persisted
```

#### Scenario: Partial failure triggers complete rollback
```
Given first entity saves successfully
When second entity save fails
Then the first entity MUST also be rolled back
And no entities SHALL be persisted
```

---

## Testing Requirements

### Requirement: UnitOfWork Unit Tests
The system SHALL include comprehensive unit tests for UnitOfWork class.

**Acceptance Criteria:**
- All public methods MUST have at least one test
- Transaction commit scenarios MUST be covered
- Transaction rollback scenarios MUST be covered
- Audit field population MUST be verified
- Code coverage MUST exceed 95%

#### Scenario: Multiple entities commit together
```
Given UnitOfWork.BeginTransactionAsync() called
When Member added
And Workspace added
And CommitTransactionAsync() called
Then both entities MUST exist in database
And both MUST have audit fields populated
```

#### Scenario: Rollback discards all changes
```
Given UnitOfWork.BeginTransactionAsync() called
When entity added
And RollbackTransactionAsync() called
Then the entity MUST NOT exist in database
And database state MUST be unchanged
```

#### Scenario: Audit fields set on SaveChangesAsync with userId
```
Given Member added to DbContext
When SaveChangesAsync("testuser") called
Then the entity.CreatedBy MUST equal "testuser"
And the entity.UpdatedBy MUST equal "testuser"
```

---

### Requirement: Application Service Integration Tests
The system SHALL include end-to-end tests for TodoService with UnitOfWork.

**Acceptance Criteria:**
- Happy path: valid data MUST create Visit and StatusHistory
- Error paths: authorization failure MUST trigger rollback
- Atomicity MUST be verified: either both succeed or both fail
- Audit fields MUST be correctly populated

#### Scenario: Valid request creates Visit and History
```
Given valid MR and HCO data exists
When CreateTodoItemAsync(mrId, hcoId) called
Then TodoItem MUST be created with correct data
And TodoStatusHistory MUST be created with correct data
And both MUST have audit fields set
```

#### Scenario: Authorization failure prevents any creation
```
Given MR is not authorized for HCO
When CreateTodoItemAsync(mrId, hcoId) called
Then UnauthorizedException MUST be thrown
And no TodoItem SHALL be created
And no TodoStatusHistory SHALL be created
And transaction MUST be rolled back completely
```

#### Scenario: Member not found triggers rollback
```
Given invalid mrId that does not exist
When CreateTodoItemAsync(invalidId, hcoId) called
Then NotFoundException MUST be thrown
And no TodoItem SHALL be created
And no TodoStatusHistory SHALL be created
```

---

## Transaction Behavior

### Requirement: ACID Compliance
The system SHALL ensure all operations maintain ACID properties.

**Acceptance Criteria:**
- **Atomicity:** All-or-nothing semantics MUST be enforced
- **Consistency:** Business rules MUST be enforced before save
- **Isolation:** Concurrent requests MUST be isolated
- **Durability:** Committed data MUST survive failures

#### Scenario: Atomicity - all or nothing
```
Given transaction begins
When first entity saved successfully
And second entity save fails
Then the first entity MUST also be rolled back
And database MUST show no changes
```

#### Scenario: Isolation - concurrent transactions don't interfere
```
Given Request A and Request B both start transactions
When Request A modifies EntityX
And Request B queries EntityX
Then Request B MUST see EntityX before Request A's commit
And isolation MUST be maintained
```

---

## Rollback Guarantees

### Requirement: Exception Handling and Rollback
The system SHALL ensure all exceptions trigger proper rollback.

**Acceptance Criteria:**
- Any exception during transaction MUST trigger rollback
- Transaction resources MUST be properly disposed
- Exception MUST be re-thrown after cleanup
- No dangling connections or transactions SHALL remain

#### Scenario: DbUpdateException triggers rollback
```
Given transaction is active
When SaveChangesAsync() throws DbUpdateException
Then RollbackTransactionAsync() MUST be automatically called
And transaction MUST be disposed
And exception MUST be re-thrown
And database MUST have no uncommitted changes
```

#### Scenario: CancellationToken cancellation triggers rollback
```
Given transaction is active
When CancellationToken is cancelled
And operation throws OperationCanceledException
Then RollbackTransactionAsync() MUST be called
And transaction MUST be disposed
And exception MUST be re-thrown
```

---

## Error Handling

### Requirement: Exception Types and Messaging
The system SHALL provide clear exception handling for different error scenarios.

**Acceptance Criteria:**
- NotFoundException MUST be thrown when entity not found
- UnauthorizedException MUST be thrown when authorization fails
- DbUpdateException MUST be re-thrown for database errors
- OperationCanceledException MUST be re-thrown for cancellations
- All exceptions MUST be properly logged and bubbled up

#### Scenario: NotFoundException on missing medical representative
```
Given TodoService tries to load non-existent MR
When CreateTodoItemAsync(invalidId, hcoId) called
Then NotFoundException MUST be thrown
And transaction MUST be rolled back
And no TodoItem SHALL be created
```

#### Scenario: UnauthorizedException on insufficient authorization
```
Given MR exists but is not authorized for HCO
When CreateTodoItemAsync(mrId, hcoId) called
Then UnauthorizedException MUST be thrown
And transaction MUST be rolled back
And no TodoItem SHALL be created
```

#### Scenario: NotFoundException on missing healthcare organization
```
Given Workspace does not exist
When CreateTodoItemAsync(validMrId, invalidHcoId) called
Then NotFoundException MUST be thrown
And transaction MUST be rolled back
And no TodoItem SHALL be created
```

---

## Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-02-04 | Architecture Team | Initial specification with delta requirements and scenarios |
