# Technical Design: Explicit Audit User Context

## Context
The Scaffold API has audit fields (`CreatedBy`, `UpdatedBy`, `DeletedBy`) on entities that should be populated with the current user's identity. Currently there is no standardized mechanism for capturing user context from the HTTP request and making it available throughout the request lifecycle. This design proposes introducing an `IAuditContext` service that provides user identity information extracted from JWT claims.

## Goals
- **Primary:** Ensure all audit fields are consistently populated with accurate user identity
- **Secondary:** Provide a single source of truth for user identity throughout a request
- **Tertiary:** Support fallback to "system" user for non-authenticated requests
- **Tertiary:** Make audit context easily testable and mockable

## Non-Goals
- Implement advanced audit features (audit log tables, change history tracking)
- Implement role-based authorization (only providing identity, not authorization)
- Support multi-tenancy (assumed single-tenant for now)
- Implement custom claim extraction (standard OIDC/JWT claims only)

## Decisions

### Decision 1: Service-Based Pattern Over Middleware
**Choice:** Use IAuditContext as a scoped service injected into Application Services

**Rationale:**
- More explicit and testable than middleware
- Clearer dependency flow (services declare what they need)
- Easier to mock in unit tests
- Allows optional audit context in some services (not all services track audit)

**Alternatives considered:**
1. Middleware that sets HttpContext items → Less explicit, harder to test
2. Static accessor (HttpContextAccessor directly in services) → No DI, harder to mock
3. Ambient context via CallContext → Not recommended in async .NET

**Selected:** IAuditContext service

---

### Decision 2: Claim Extraction Strategy
**Choice:** Extract UserId from `ClaimTypes.NameIdentifier`, UserName from `ClaimTypes.Name`

**Rationale:**
- Standard OIDC/JWT claim names
- Compatible with Azure AD, Auth0, and other identity providers
- IHttpContextAccessor provides access to HttpContext.User.Claims
- Easy to override with custom claims in the future

**Alternatives considered:**
1. Custom claim names (e.g., "userId", "email") → Less portable, requires configuration
2. Always use email as UserId → Not all tokens include email, less unique
3. Extract multiple claim types with fallback chain → Added complexity

**Selected:** Standard ClaimTypes

**Implementation:**
```csharp
public string UserId 
{
    get 
    {
        var claim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value ?? "system";
    }
}
```

---

### Decision 3: Fallback Behavior
**Choice:** Fallback to "system" for anonymous/unauthenticated requests

**Rationale:**
- Some requests (health checks, swagger, public endpoints) may not require auth
- "system" indicates an automatic operation vs user-initiated
- Easier to audit and distinguish automated changes from user actions
- Consistent with industry patterns (e.g., WordPress "system" user)

**Alternatives considered:**
1. Throw exception if no user → Breaks non-authenticated requests
2. Return null → Requires null handling everywhere
3. Return empty string → Ambiguous, hard to distinguish from missing claims

**Selected:** Fallback to "system"

---

### Decision 4: Scope and Lifetime
**Choice:** Register IAuditContext as a scoped service

**Rationale:**
- One instance per HTTP request
- User identity is request-specific
- Matches DI lifetime of DbContext and other request-scoped services
- Ensures consistency within a single request

**Alternatives considered:**
1. Singleton → Would capture identity once at startup
2. Transient → Unnecessary object creation per injection point

**Selected:** Scoped

---

### Decision 5: Integration with UnitOfWork
**Choice:** IAuditContext remains independent; UnitOfWork accepts optional userId parameter

**Rationale:**
- Separation of concerns: IAuditContext provides identity, UnitOfWork manages transactions
- Application Services orchestrate the two: call IAuditContext.UserId, pass to UnitOfWork
- Makes UnitOfWork reusable in non-HTTP contexts (e.g., background jobs)

**Alternatives considered:**
1. Make UnitOfWork depend on IAuditContext → Couples patterns, hard to use outside HTTP
2. Set audit fields in IAuditContext → Violates single responsibility

**Selected:** Independent services with Application Service orchestration

---

## Architecture Diagram

```
HTTP Request
    ↓
Controller (MediatR)
    ↓
Handler (injects IAuditContext)
    ↓
Application Service (injects IAuditContext + IUnitOfWork)
    ├─→ Get userId from IAuditContext
    ├─→ Create/modify entities
    ├─→ Call UnitOfWork.BeginTransactionAsync()
    ├─→ UnitOfWork.SaveChangesAsync(userId)
    │   └─→ SetAuditFields(userId)
    │       ├─→ CreatedBy = userId
    │       ├─→ UpdatedBy = userId
    │       └─→ DeletedBy = userId (on delete)
    └─→ UnitOfWork.CommitTransactionAsync()

IAuditContext (scoped)
    ├─→ Injects IHttpContextAccessor
    ├─→ Extracts claims from HttpContext.User
    └─→ Returns UserId, UserName, UtcNow
```

---

## Data Flow

### Scenario: Create Visit Request
1. HTTP POST /visits with JWT token
2. Controller receives request, creates CreateTodoItemCommand
3. MediatR routes to CreateTodoItemHandler
4. Handler injects IAuditContext
5. Application Service injects IAuditContext + IUnitOfWork
6. Create TodoItem entity (CreatedBy = null initially)
7. Begin transaction: UnitOfWork.BeginTransactionAsync()
8. Add entity to DbContext
9. Get userId: `var userId = _auditContext.UserId`
10. Save with audit: UnitOfWork.SaveChangesAsync(userId)
11. SetAuditFields sets CreatedBy = userId
12. Commit: UnitOfWork.CommitTransactionAsync()
13. Return response

### Scenario: Soft Delete with Audit
1. HTTP DELETE /visits/{id} with JWT token
2. Controller receives request
3. Handler injects IAuditContext
4. Service calls domain service: SoftDeleteDomainService.MarkAsDeleted(entity, userId)
5. MarkAsDeleted sets DeletedBy = userId, DeletedAt = now, IsDeleted = true
6. Save via UnitOfWork.SaveChangesAsync(userId)
7. Commit transaction

---

## Testing Strategy

### Unit Tests: AuditContextService
- **Test 1:** Authenticated user → Returns UserId from ClaimTypes.NameIdentifier
- **Test 2:** Authenticated user → Returns UserName from ClaimTypes.Name
- **Test 3:** Anonymous user (no claims) → Returns "system" for both
- **Test 4:** Missing ClaimTypes.NameIdentifier → Returns "system"
- **Test 5:** Missing ClaimTypes.Name → Returns "system"
- **Test 6:** HttpContext is null → Returns "system"
- **Test 7:** UtcNow returns valid DateTime

### Integration Tests: Audit Field Population
- **Test 1:** Create entity → CreatedBy = userId, UpdatedBy = userId
- **Test 2:** Modify entity → UpdatedBy updated, CreatedBy unchanged
- **Test 3:** Anonymous request → CreatedBy = "system"
- **Test 4:** Multiple entities in transaction → All have correct audit fields
- **Test 5:** Soft delete → DeletedBy = userId, IsDeleted = true

### Mocking in Tests
```csharp
// Mock IAuditContext for isolated service tests
var mockAuditContext = new Mock<IAuditContext>();
mockAuditContext.Setup(x => x.UserId).Returns("test-user-123");
mockAuditContext.Setup(x => x.UserName).Returns("John Doe");
mockAuditContext.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
```

---

## Implementation Checklist
- [ ] Create IAuditContext interface
- [ ] Create AuditContextService implementation
- [ ] Register in DI container
- [ ] Inject into Application Services
- [ ] Update UnitOfWork integration
- [ ] Write comprehensive tests (>95% coverage)
- [ ] Verify backward compatibility
- [ ] Document usage patterns

---

## Risk Analysis

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Missing claims in token | Medium | Low | Fallback to "system", document expected claims |
| HttpContext null in edge cases | Low | Low | Null-coalesce operator, test edge cases |
| Performance (per-request extraction) | Low | Very Low | Cached in scoped service, minimal overhead |
| Breaking change if modifying claim names | Low | Medium | Document claim extraction, test extensively |
| Concurrent request isolation | Very Low | High | Scoped service per request, no static state |

---

## Migration Plan
**No migration needed:** This is additive-only; existing code continues to work.

**Phased adoption:**
1. Phase 1: Deploy IAuditContext service (no behavioral changes)
2. Phase 2: Gradually inject into services as they are modified
3. Phase 3: Update all Application Services in one release
4. Phase 4: Establish standard pattern for future services

---

## Open Questions
- Q: Should we support custom claim extraction? A: Not in this phase; can add in future if needed
- Q: Should we audit the audit fields themselves (who set CreatedBy)? A: No, out of scope; consider separate "audit log" feature
- Q: Should we validate that UserId exists in a users table? A: No, keep service focused on identity extraction

---

## Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-02-05 | Architecture Team | Initial design with claim extraction strategy and testing approach |
