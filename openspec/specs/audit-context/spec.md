# audit-context Specification

## Purpose
Defines how request user identity is exposed through IAuditContext and applied to CreatedBy, UpdatedBy, and DeletedBy audit fields.
## Requirements
### Requirement: IAuditContext Interface
The system SHALL provide an IAuditContext interface for accessing current user identity and timestamp information throughout the request lifecycle.

**Acceptance Criteria:**
- Interface MUST be located in ForgeKit.Api/Interfaces/IAuditContext.cs
- Interface MUST expose three properties: UserId (string), UserName (string), UtcNow (DateTime)
- All properties MUST return non-null values (fallback to "system" if not available)
- Interface MUST be marked as scoped in DI

#### Scenario: Application Service retrieves user identity
- **WHEN** a controller injects IAuditContext
- **THEN** the service accesses _auditContext.UserId
- **THEN** it SHALL receive a non-null string representing the current user's ID

#### Scenario: System user fallback when no authentication
- **WHEN** a request is made without authentication (anonymous)
- **THEN** IAuditContext.UserId is accessed
- **THEN** it SHALL return "system" as fallback

---

### Requirement: AuditContextService Implementation
The system SHALL implement AuditContextService that extracts user identity from HTTP context claims.

**Acceptance Criteria:**
- Class MUST be located in ForgeKit.Api/Services/AuditContextService.cs
- Class MUST implement IAuditContext
- Class MUST inject IHttpContextAccessor via constructor
- UserId property MUST extract from ClaimTypes.NameIdentifier
- UserName property MUST extract from ClaimTypes.Name
- Both properties MUST fallback to "system" if claim not present
- UtcNow property MUST return DateTime.UtcNow

#### Scenario: Authenticated user provides UserId from token
- **WHEN** a JWT token with ClaimTypes.NameIdentifier = "user-123"
- **THEN** AuditContextService.UserId is accessed
- **THEN** it SHALL return "user-123"

#### Scenario: Authenticated user provides UserName from token
- **WHEN** a JWT token with ClaimTypes.Name = "John Doe"
- **THEN** AuditContextService.UserName is accessed
- **THEN** it SHALL return "John Doe"

#### Scenario: Anonymous request falls back to system user
- **WHEN** HttpContext.User has no claims
- **THEN** AuditContextService.UserId is accessed
- **THEN** it SHALL return "system"

#### Scenario: Missing claim falls back to system
- **WHEN** HttpContext.User is null or missing NameIdentifier claim
- **THEN** AuditContextService.UserId is accessed
- **THEN** it SHALL return "system"

#### Scenario: UtcNow returns current timestamp
- **WHEN** AuditContextService is instantiated
- **THEN** UtcNow property is accessed
- **THEN** it SHALL return a DateTime value representing the current UTC time

---

### Requirement: Audit Context Service Registration
The system SHALL register AuditContextService in the dependency injection container.

**Acceptance Criteria:**
- IAuditContext MUST be registered as a scoped service
- Implementation MUST be AuditContextService
- Registration MUST occur in Program.cs in the Data/Services section
- IHttpContextAccessor MUST be available for injection

#### Scenario: Application Service receives IAuditContext via DI
- **WHEN** Program.cs has registered IAuditContext as scoped
- **THEN** Application Service constructor requests IAuditContext
- **THEN** an instance of AuditContextService MUST be injected
- **THEN** it MUST have access to configured IHttpContextAccessor

#### Scenario: Each HTTP request gets independent audit context
- **WHEN** two HTTP requests are processed
- **THEN** both request IAuditContext
- **THEN** each SHALL receive a separate scoped instance
- **THEN** audit contexts SHALL NOT share state between requests

---

### Requirement: Audit Field Population Using IAuditContext
The system SHALL populate CreatedBy, UpdatedBy, and DeletedBy fields using IAuditContext automatically.

**Acceptance Criteria:**
- All new entities MUST have CreatedBy = IAuditContext.UserId
- All modified entities MUST have UpdatedBy = IAuditContext.UserId
- All soft-deleted entities MUST have DeletedBy = IAuditContext.UserId
- Application Services MUST NOT manually set audit fields
- Audit field population MUST be consistent across all services

#### Scenario: New entity gets CreatedBy from audit context
- **WHEN** a TodoItem entity is created
- **THEN** saved via UnitOfWork.SaveChangesAsync(auditContext.UserId)
- **THEN** the entity.CreatedBy MUST equal auditContext.UserId
- **THEN** the entity.UpdatedBy MUST equal auditContext.UserId

#### Scenario: Modified entity updates only UpdatedBy
- **WHEN** an existing entity with CreatedBy = "user-1"
- **THEN** the entity is modified
- **THEN** saved via UnitOfWork.SaveChangesAsync(auditContext.UserId) where auditContext.UserId = "user-2"
- **THEN** the entity.CreatedBy MUST remain "user-1"
- **THEN** the entity.UpdatedBy MUST equal "user-2"

#### Scenario: Soft delete sets DeletedBy from audit context
- **WHEN** an entity exists in database
- **THEN** SoftDeleteDomainService.MarkAsDeleted(entity, auditContext.UserId) called
- **THEN** the entity.DeletedBy MUST equal auditContext.UserId
- **THEN** the entity.IsDeleted MUST be true
- **THEN** the entity.DeletedAt MUST be set

#### Scenario: Anonymous request uses system user for audit fields
- **WHEN** an anonymous request (no JWT token)
- **THEN** entity is created
- **THEN** the entity.CreatedBy MUST equal "system"

---

### Requirement: Application Service Integration
The system SHALL integrate IAuditContext into Application Services for consistent audit field handling.

**Acceptance Criteria:**
- TodoService MUST inject IAuditContext
- CreateTodoAsync MUST use _auditContext.UserId for audit fields
- All entity creation MUST include audit context
- DomainService calls MUST pass userId from IAuditContext

#### Scenario: TodoService uses audit context for creation
- **WHEN** TodoService.CreateTodoAsync is called
- **THEN** a TodoItem is created
- **THEN** _auditContext.UserId MUST be used for audit fields
- **THEN** the saved entity MUST have CreatedBy = _auditContext.UserId

#### Scenario: Domain service receives userId from application service
- **WHEN** SoftDeleteDomainService.MarkAsDeleted is called
- **THEN** passed userId = auditContext.UserId
- **THEN** the entity.DeletedBy MUST equal that userId

#### Scenario: Multiple operations in transaction use same audit context
- **WHEN** a transaction saves multiple entities
- **THEN** UnitOfWork.SaveChangesAsync(auditContext.UserId) called
- **THEN** all entities MUST have audit fields set to same auditContext.UserId

---

### Requirement: Audit Context in Error Scenarios
The system SHALL maintain proper audit context even during error conditions.

**Acceptance Criteria:**
- Audit context MUST be available even if entity creation fails
- Audit context MUST not be null on exceptions
- System MUST fallback gracefully with "system" user

#### Scenario: Error does not corrupt audit context
- **WHEN** entity creation fails with validation error
- **THEN** rolled back
- **THEN** audit context MUST still be accessible
- **THEN** next successful operation MUST have correct audit fields

---
