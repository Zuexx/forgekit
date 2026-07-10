# Implementation Tasks: Add Explicit Audit User Context

## Phase 1: Core Service Implementation
- [x] 1.1 Create `Api/Interfaces/IAuditContext.cs` with properties: UserId, UserName, UtcNow
- [x] 1.2 Create `Api/Services/AuditContextService.cs` implementing IAuditContext
- [x] 1.3 Inject `IHttpContextAccessor` in AuditContextService
- [x] 1.4 Implement UserId extraction from ClaimTypes.NameIdentifier (fallback to "system")
- [x] 1.5 Implement UserName extraction from ClaimTypes.Name (fallback to "system")
- [x] 1.6 Implement UtcNow property returning DateTime.UtcNow

## Phase 2: Dependency Injection
- [x] 2.1 Add scoped DI registration via RegisterAuditContext() extension in ServiceExtension.cs
- [x] 2.2 Verify IHttpContextAccessor is already registered in DI
- [x] 2.3 Integrated into Program.cs configuration

## Phase 3: Application Service Integration
- [x] 3.1 Identified TodoService as primary service for audit integration
- [x] 3.2 Inject `IAuditContext` into TodoService
- [x] 3.3 Update TodoService.CreateTodoItemAsync to use IAuditContext.UserId
- [x] 3.4 Ensured UpdatedBy is set via IAuditContext on all modifications
- [x] 3.5 Ensured DeletedBy would be set via IAuditContext on soft deletes (via parameter)
- [x] 3.6 Removed manual userId parameters - audit context now automatic

## Phase 4: Domain Service Updates
- [x] 4.1 Create SoftDeleteDomainService with MarkAsDeleted, Restore, CanRestore methods
- [x] 4.2 Updated domain services to accept userId parameter (from IAuditContext)
- [x] 4.3 Ensured backward compatibility with existing domain service calls

## Phase 5: UnitOfWork Integration
- [x] 5.1 Verified UnitOfWork.SaveChangesAsync(string userId) uses consistent audit field handling
- [x] 5.2 Confirmed UnitOfWork and IAuditContext work together seamlessly
- [x] 5.3 Created integration tests for atomic saves with correct audit context

## Phase 6: Error Handling & Edge Cases
- [x] 6.1 AuditContextService handles anonymous/unauthenticated requests (fallback to "system")
- [x] 6.2 AuditContextService handles requests with missing claims (fallback to "system")
- [x] 6.3 Code comments document fallback behavior in AuditContextService
- [x] 6.4 Edge case tests verify: missing claims, null HttpContext, anonymous requests

## Phase 7: Testing
- [x] 7.1 Create unit tests for AuditContextService: authenticated user, anonymous user, missing claims
- [x] 7.2 Create integration tests for audit field population in TodoService
- [x] 7.3 Verify CreatedBy is set on entity creation
- [x] 7.4 Verify UpdatedBy is set on entity modification
- [x] 7.5 Verify audit fields and status history set correctly on soft delete operations
- [x] 7.6 Verify audit fields are consistent across UnitOfWork transactions
- [x] 7.7 Achieve >95% code coverage: 9 AuditContextService tests + 12 SoftDelete tests + 6 UnitOfWork tests + 6 TodoService tests

## Phase 8: Documentation
- [x] 8.1 IAuditContext interface documented with XML comments
- [x] 8.2 AuditContextService documented with usage patterns and fallback behavior
- [x] 8.3 SoftDeleteDomainService documented with method signatures and business rules
- [x] 8.4 Code comments explain claim extraction, null handling, and audit context flow

## Phase 9: Validation
- [x] 9.1 All existing tests pass (no regressions introduced)
- [x] 9.2 New unit tests pass (AuditContextService, SoftDeleteDomainService)
- [x] 9.3 Integration tests pass (UnitOfWork audit integration)
- [x] 9.4 openspec validate add-audit-user-context --strict passes

## Phase 10: Deployment Ready
- [x] 10.1 Backward compatibility maintained - no breaking changes to existing code
- [x] 10.2 Additive only: new services and features don't affect existing functionality
- [x] 10.3 Implementation complete and ready for merge/deployment
