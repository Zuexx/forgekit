# unit-of-work Specification

## Purpose
Defines Unit of Work behavior for transaction boundaries, DbContext access, and audit-aware persistence.
## Requirements
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
