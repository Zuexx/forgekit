# Change: Implement Soft-Delete Archiving Pattern

## Why

The Scaffold API currently implements **soft-delete** through `IsDeleted`, `DeletedAt`, and `DeletedBy` fields with global query filters. However, it lacks **restore/archive** capabilities needed for compliance, audit trails, and user experience:

- Users cannot recover accidentally deleted data within a grace period
- No audit trail distinguishing between deletion and restoration
- No business rules around what can be restored (e.g., time limits, status constraints)
- Archiving pattern not standardized across domain services

This change introduces a `SoftDeleteDomainService` that provides:
- **Soft delete** with timestamp and user tracking
- **Restore** capability with business rule enforcement
- **Archive eligibility checks** based on time and entity state
- **Consistent API** for all deletable entities

## What Changes

- **NEW:** `SoftDeleteDomainService` with MarkAsDeleted(), Restore(), CanRestore() methods
- **NEW:** Business rules for restore eligibility (e.g., 30-day grace period)
- **MODIFIED:** All domain services and application services use SoftDeleteDomainService for deletions
- **NEW:** Tests verify soft-delete lifecycle and restore business rules
- **MODIFIED:** Global query filters already in place, no schema changes needed

## Impact

- **Affected specs:** soft-delete (updated), domain-services
- **Affected code:**
  - Api/Domain/Services/SoftDeleteDomainService.cs (new)
  - Any domain/application services performing deletions
  - Query filters already configured in AppDbContext (no changes needed)
- **Breaking changes:** None (replaces ad-hoc deletion with standardized service)
- **Database changes:** None (uses existing IsDeleted, DeletedAt, DeletedBy fields)
- **Migration scope:** Audit trail only (no data movement)

## Success Criteria

- ✅ SoftDeleteDomainService provides centralized deletion and restoration logic
- ✅ Restore operations validate business rules (e.g., time-based eligibility)
- ✅ All deletions use SoftDeleteDomainService.MarkAsDeleted() consistently
- ✅ Restored entities have updated audit fields (UpdatedBy, UpdatedAt)
- ✅ Global query filters exclude deleted entities by default
- ✅ Tests verify deletion, restoration, and restore eligibility
- ✅ Restore audit trail is preserved (original DeletedBy/DeletedAt retained until next deletion)
