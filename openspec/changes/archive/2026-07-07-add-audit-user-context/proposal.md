# Change: Add Explicit Audit User Context Service

## Why
The Scaffold API has `CreatedBy`, `UpdatedBy`, and `DeletedBy` audit fields on entities, but there is **no standardized way to capture and pass the current user's identity** throughout the request lifecycle. Currently, these fields may be NULL or populated manually in multiple places, creating inconsistency and audit trail integrity issues.

This change introduces an `IAuditContext` service that automatically extracts and provides user identity information from the HTTP context, ensuring all audit fields are consistently populated at creation, modification, and deletion time.

## What Changes
- **NEW:** `IAuditContext` interface for accessing current user identity and timestamp
- **NEW:** `AuditContextService` implementation extracting claims from JWT tokens
- **NEW:** DI registration of `AuditContextService` as scoped service
- **MODIFIED:** Application Services now inject `IAuditContext` to populate audit fields consistently
- **NEW:** Domain Services accept optional userId parameter for audit operations
- **NEW:** Tests verify audit context extraction and audit field population

## Impact
- **Affected specs:** audit-context (new), and any existing specs that use audit fields
- **Affected code:** 
  - Api/Services/AuditContextService.cs (new)
  - Api/Interfaces/IAuditContext.cs (new)
  - Program.cs (DI registration)
  - Api/Domain/Services/* (modified to use IAuditContext)
  - All Application Services using audit fields
- **Breaking changes:** None (additive only, backward compatible)
- **Database changes:** None

## Success Criteria
- ✅ IAuditContext service provides UserId, UserName, and UtcNow
- ✅ All audit field population uses IAuditContext automatically
- ✅ Application Services never create entities without audit context
- ✅ Audit trail integrity is verified through tests
- ✅ System and fallback users handled for non-authenticated requests
