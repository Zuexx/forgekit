# ADR-005: Audit Context Service with Automatic Claims Extraction

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-002, ADR-004, ADR-007

## Context

Modern web applications require audit trails for compliance, security, and operational needs. Questions arise:

- **Who changed this record?** (required for compliance auditing)
- **When was it changed?** (required for investigation and debugging)
- **How do we capture user information automatically?** (without asking developers)
- **Where do we get the user ID from?** (JWT claims, session, HTTP headers?)
- **How do we ensure consistency?** (all audit fields populated the same way)

ForgeKit requirements:
- Automatic audit field population (CreatedBy, UpdatedBy, DeletedBy)
- Extract user information from JWT claims (NameIdentifier, Name)
- Provide consistent audit context throughout request lifecycle
- Handle unauthenticated requests gracefully (fallback to "system")
- Enable feature flags and compliance tracking
- Work seamlessly with database transactions

## Decision

Implement **IAuditContext Service** that:

1. **Scoped Service:** One instance per HTTP request
2. **Extract JWT Claims:** Pulls UserId from ClaimTypes.NameIdentifier
3. **Consistent Timestamps:** All entities use same UtcNow for request
4. **Graceful Fallback:** Returns "system" for unauthenticated requests
5. **Automatic Population:** IUnitOfWork.SaveChangesAsync(userId) populates audit fields
6. **Simple Interface:** Only 3 properties (UserId, UserName, UtcNow)

### IAuditContext Interface

```csharp
public interface IAuditContext
{
    /// <summary>
    /// Gets the current user's ID from the JWT token's NameIdentifier claim.
    /// Falls back to "system" if no authenticated user is present.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the current user's name from the JWT token's Name claim.
    /// Falls back to "system" if no authenticated user is present.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Gets the current UTC timestamp.
    /// Used for populating audit fields like CreatedAt, UpdatedAt, DeletedAt.
    /// </summary>
    DateTime UtcNow { get; }
}
```

### AuditContextService Implementation

```csharp
public class AuditContextService : IAuditContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ??
            throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string UserId
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier);
                return claim?.Value ?? "system";
            }
            catch
            {
                return "system";
            }
        }
    }

    public string UserName
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.Name);
                return claim?.Value ?? "system";
            }
            catch
            {
                return "system";
            }
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;
}
```

## Rationale

### Why IAuditContext Service?

1. **Separation of Concerns:**
   - Claims extraction isolated from business logic
   - Services don't directly access HttpContext
   - Audit logic in one place (easy to change)

2. **Scoped Lifetimes:**
   - One instance per HTTP request ensures consistency
   - All entities modified in request have same UserId
   - Timestamps consistent within request lifecycle
   - Prevents timing issues with multi-step operations

3. **Graceful Degradation:**
   - Non-authenticated requests work with "system" fallback
   - Health checks, scheduled jobs don't need auth
   - No null reference exceptions
   - Test-friendly (doesn't require HttpContext)

4. **JWT Claims Standard:**
   - Uses standard ClaimTypes.NameIdentifier (not custom claim)
   - Aligns with .NET JWT implementations
   - Works with any JWT provider (IdentityServer, Auth0, Azure AD, etc.)
   - Claims extracted at authentication middleware (already trusted)

5. **Automatic Audit Field Management:**
   ```csharp
   // No need to manually set CreatedBy, UpdatedBy, DeletedBy
   await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);
   // IUnitOfWork automatically populates audit fields
   ```

6. **Type Safety:**
   - Interface-based (mockable)
   - Injected through DI (testable)
   - Intellisense support
   - No magic strings

7. **Testability:**
   - Can mock IAuditContext for unit tests
   - Can set fake HttpContext for integration tests
   - Tests control audit trail explicitly

8. **Performance:**
   - Minimal overhead (claim lookup on property access)
   - HttpContext access is fast
   - No database queries needed
   - Cached claims by default in HttpContext

### Why Not Alternatives?

The following approaches were considered and rejected:

## Alternatives Considered

### 1. Static/Global Audit Context
**Approach:** Use static properties or ambient context

```csharp
public static class AuditContext
{
    public static string UserId { get; set; }
    public static string UserName { get; set; }
    public static DateTime UtcNow { get; set; }
}

// Usage
AuditContext.UserId = user.Id;
var entity = new TodoItem { CreatedBy = AuditContext.UserId };
```

**Pros:**
- Simple, no dependency injection
- Easy to access anywhere
- Works in static contexts

**Cons:**
- Not thread-safe in concurrent scenarios
- Difficult to test (shared mutable state)
- Difficult to isolate per-request in tests
- Requires manual population before each operation
- Can be accidentally shared across requests
- Anti-pattern in async/multi-threaded environments
- Not suitable for cloud/serverless

**When Better:** Single-threaded desktop applications (not web APIs)

---

### 2. Manual Claim Extraction in Each Service
**Approach:** Extract claims in every service that needs audit info

```csharp
public class TodoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<TodoItem> CreateTodoAsync(...)
    {
        var userId = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

        var visit = new TodoItem { CreatedBy = userId };
        // ... rest of logic
    }
}
```

**Pros:**
- Explicit about audit trail
- No additional abstraction

**Cons:**
- Code duplication (every service repeats claim extraction)
- Easy to make mistakes (forget claim type)
- Hard to maintain (change claim type → update everywhere)
- Couples services to HttpContext
- Couples to ClaimTypes constants
- Testability harder (mock HttpContext in every test)
- Not DRY

**When Better:** Very small application (< 3 services)

---

### 3. Audit Attribute on Properties
**Approach:** Use attributes to mark properties for automatic audit population

```csharp
public class TodoItem : BaseEntity
{
    [Audit(AuditField.CreatedBy)]
    public string CreatedBy { get; set; }

    [Audit(AuditField.UpdatedBy)]
    public string UpdatedBy { get; set; }
}

// Middleware or SaveChanges intercepts, populates marked fields
```

**Pros:**
- Declarative syntax
- Clear intent on properties
- Framework handles population

**Cons:**
- Requires reflection in SaveChanges
- Hides logic (where is it populated?)
- Less flexible (can't vary behavior)
- Not standard .NET approach
- Harder to test

**When Better:** Metadata-driven frameworks

---

### 4. Middleware-Based Audit Context
**Approach:** Set audit context in middleware before request is processed

```csharp
public class AuditContextMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, AuditContextService auditService)
    {
        // Extract claims in middleware
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        AuditContext.UserId = userId; // Store in HttpContext.Items or ambient context

        await _next(context);
    }
}
```

**Pros:**
- Centralized claim extraction
- One place to change logic

**Cons:**
- Requires middleware registration
- Magic context storage (HttpContext.Items or ambient)
- Easier to forget HttpContext.Items cleanup
- Less clear dependency (implicit)
- Can create multiple instances if not careful

**When Better:** Framework-level concerns

---

### 5. Service Factory Pattern
**Approach:** Factory creates services with pre-populated audit context

```csharp
public interface IServiceFactory
{
    T CreateService<T>() where T : IHasAuditContext;
}

public class ServiceFactory : IServiceFactory
{
    private readonly IAuditContext _auditContext;

    public T CreateService<T>() where T : IHasAuditContext
    {
        var service = Activator.CreateInstance<T>();
        service.AuditContext = _auditContext;
        return service;
    }
}

// Usage
var service = _serviceFactory.CreateService<TodoService>();
```

**Pros:**
- Explicit about audit context population
- Can vary creation per service

**Cons:**
- Requires special factory
- More boilerplate
- Less standard (DI containers don't support this)
- Harder to test
- Over-engineered

**When Better:** Legacy code; non-DI frameworks

---

### 6. Request Scope/Context in HttpContext.Items
**Approach:** Store audit info in HttpContext.Items dictionary

```csharp
// In middleware or controller
HttpContext.Items["AuditContext"] = new AuditContextData
{
    UserId = "user-123",
    Timestamp = DateTime.UtcNow
};

// In service
var auditContext = (AuditContextData)HttpContext.Items["AuditContext"];
entity.CreatedBy = auditContext.UserId;
```

**Pros:**
- Built-in HttpContext mechanism
- Automatically scoped to request

**Cons:**
- Magic string keys (error-prone)
- Requires casting
- Implicit behavior
- Not type-safe
- Manual population needed
- Not mockable for unit tests
- Coupling to HttpContext

**When Better:** Simple web forms applications

---

## Consequences

### Positive

1. **Automatic Audit Trails:** CreatedBy, UpdatedBy, DeletedBy populated automatically
2. **Consistent User Tracking:** All operations in request attributed to same user
3. **Zero Boilerplate:** Services don't write audit code, it's automatic
4. **Easy Testing:** Mock IAuditContext for unit tests
5. **Testable Audit Logic:** Can test AuditContextService independently
6. **Graceful Fallback:** Non-authenticated requests work (fallback to "system")
7. **Standard Claims:** Uses .NET standard ClaimTypes
8. **Clear Dependency:** Services explicitly depend on IAuditContext
9. **Compliance Ready:** Complete audit trail for regulatory requirements
10. **DRY:** Audit extraction logic in one place

### Negative

1. **HttpContext Dependency:** Service depends on HTTP context presence
2. **Per-Request Service:** Scoped lifetime adds slight overhead
3. **Claims Dependency:** Assumes JWT/Claims-based authentication
4. **Testing Setup:** Tests need to mock IAuditContext
5. **Claim Name Coupling:** Uses ClaimTypes.NameIdentifier (could be different in custom setups)
6. **Silent Fallback:** "system" user for non-authenticated requests could hide issues

### Neutral

1. **Abstraction Layer:** Adds service layer, but necessary for separation
2. **HTTP-specific:** Not useful outside HTTP request context (but that's the target)

## When to Use

✅ **Use IAuditContext Service when:**
- Building web API with authentication
- Need automatic audit trails
- Using JWT or Claims-based authentication
- Want to track who created/modified/deleted records
- Compliance/regulatory requirements
- Want clean services without audit code duplication
- Using ASP.NET Core with DI container
- Building multi-user system

✅ **Specifically for:**
- ASP.NET Core applications with JWT authentication
- Any system requiring audit trails
- Compliance-regulated systems (healthcare, finance, etc.)

## When NOT to Use

❌ **Avoid IAuditContext Service when:**
- Building non-HTTP service (console app, background job without HttpContext)
- Audit trails not required
- User context not available (system-to-system integration)
- Not using claims-based authentication
- Static/global audit context required for some reason
- Simple application with no audit requirements

❌ **Don't use for:**
- Non-authenticated requests needing real user tracking
- Services that can't depend on IHttpContextAccessor
- Applications without HTTP request context

## ForgeKit Implementation

### Interface Definition

```csharp
// ForgeKit.Api/Interfaces/IAuditContext.cs
namespace Api.Interfaces;

/// <summary>
/// Provides audit context information (user identity and timestamp) for the current request.
/// Scoped service - one instance per HTTP request.
/// </summary>
public interface IAuditContext
{
    /// <summary>
    /// Gets the current user's ID from JWT token's NameIdentifier claim.
    /// Falls back to "system" if no authenticated user or claim is missing.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the current user's name from JWT token's Name claim.
    /// Falls back to "system" if no authenticated user or claim is missing.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Gets the current UTC timestamp for audit fields.
    /// </summary>
    DateTime UtcNow { get; }
}
```

### Service Implementation

```csharp
// ForgeKit.Api/Services/AuditContextService.cs
using System.Security.Claims;
using Api.Interfaces;

namespace Api.Services;

/// <summary>
/// Scoped service that extracts user identity from JWT claims.
/// </summary>
public class AuditContextService : IAuditContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ??
            throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Extracts user ID from ClaimTypes.NameIdentifier claim.
    /// Returns "system" if no authenticated user or claim missing.
    /// </summary>
    public string UserId
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier);
                return claim?.Value ?? "system";
            }
            catch
            {
                // Handle HttpContext access errors gracefully
                return "system";
            }
        }
    }

    /// <summary>
    /// Extracts user name from ClaimTypes.Name claim.
    /// Returns "system" if no authenticated user or claim missing.
    /// </summary>
    public string UserName
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.Name);
                return claim?.Value ?? "system";
            }
            catch
            {
                // Handle HttpContext access errors gracefully
                return "system";
            }
        }
    }

    /// <summary>
    /// Returns current UTC time for audit fields.
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
```

### Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Register IHttpContextAccessor (required for IAuditContext)
builder.Services.AddHttpContextAccessor();

// Register AuditContextService as scoped (one per request)
builder.Services.AddScoped<IAuditContext, AuditContextService>();

// ... rest of configuration
```

### Usage in Services

```csharp
// ForgeKit.Api/Services/Visits/TodoService.cs
public class TodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditContext _auditContext;
    private readonly SoftDeleteDomainService _softDeleteService;

    public TodoService(
        IUnitOfWork unitOfWork,
        IAuditContext auditContext,
        SoftDeleteDomainService softDeleteService)
    {
        _unitOfWork = unitOfWork;
        _auditContext = auditContext;
        _softDeleteService = softDeleteService;
    }

    public async Task<TodoItem> CreateTodoAsync(
        string mrId,
        string hcoId,
        DateTime visitDate,
        string? purpose = null,
        CancellationToken ct = default)
    {
        // ... validation and business logic ...

        var todoItem = new TodoItem
        {
            MrId = mrId,
            HcoId = hcoId,
            VisitDate = visitDate,
            Purpose = purpose,
            CurrentStatus = "Requested"
        };

        _unitOfWork.DbContext.Add(todoItem);

        // Create status history with audit context
        var statusHistory = new TodoStatusHistory
        {
            TodoItemId = todoItem.Id,
            Status = "Requested",
            Timestamp = _auditContext.UtcNow,  // From IAuditContext
            ChangedBy = _auditContext.UserId,   // From IAuditContext
            Notes = "Visit request created"
        };

        _unitOfWork.DbContext.Add(statusHistory);

        // IUnitOfWork automatically populates CreatedBy, UpdatedBy using _auditContext.UserId
        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);

        return todoItem;
    }

    public async Task<TodoItem> DeleteTodoItemAsync(
        string todoItemId,
        CancellationToken ct = default)
    {
        var todoItem = await _unitOfWork.DbContext.Set<TodoItem>()
            .FirstOrDefaultAsync(v => v.Id == todoItemId, cancellationToken: ct);

        if (todoItem == null)
            throw new BusinessLogicException($"TodoItem '{todoItemId}' not found");

        // Domain service marks as deleted with audit info
        _softDeleteService.MarkAsDeleted(todoItem, _auditContext.UserId);

        await _unitOfWork.SaveChangesAsync(_auditContext.UserId, ct);

        return todoItem;
    }
}
```

### Unit Test with Mocked IAuditContext

```csharp
// ForgeKit.Api.Tests/Services/TodoServiceAuditTests.cs
[TestFixture]
public class TodoServiceAuditTests
{
    private TodoService _service;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IAuditContext> _auditContextMock;
    private SoftDeleteDomainService _softDeleteService;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditContextMock = new Mock<IAuditContext>();
        _softDeleteService = new SoftDeleteDomainService();

        // Configure audit context mock
        _auditContextMock.Setup(x => x.UserId).Returns("test-user-123");
        _auditContextMock.Setup(x => x.UserName).Returns("Test User");
        _auditContextMock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 2, 9, 12, 0, 0));

        _service = new TodoService(
            _unitOfWorkMock.Object,
            _auditContextMock.Object,
            _softDeleteService);
    }

    [Test]
    public async Task CreateTodoItem_PopulatesStatusHistoryWithAuditContext()
    {
        // Arrange
        var visitDate = DateTime.UtcNow.AddDays(5);
        var todoItem = new TodoItem();
        var mockDbContext = new Mock<AppDbContext>();

        _unitOfWorkMock.Setup(x => x.DbContext).Returns(mockDbContext.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // ... setup mocks ...

        // Act
        var result = await _service.CreateTodoAsync("mr-1", "hco-1", visitDate, "Training");

        // Assert
        // Verify SaveChangesAsync was called with correct user ID
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync("test-user-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Test with Real HttpContext

```csharp
// ForgeKit.Api.Tests/Services/AuditContextServiceTests.cs
[TestFixture]
public class AuditContextServiceTests
{
    private AuditContextService _service;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _service = new AuditContextService(_httpContextAccessorMock.Object);
    }

    [Test]
    public void UserId_WithValidNameIdentifierClaim_ReturnsClaim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(identity);

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var userId = _service.UserId;

        // Assert
        Assert.That(userId, Is.EqualTo("user-123"));
    }

    [Test]
    public void UserId_WithNoClaim_ReturnsFallback()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var userId = _service.UserId;

        // Assert
        Assert.That(userId, Is.EqualTo("system"));
    }

    [Test]
    public void UserId_WithNullHttpContext_ReturnsFallback()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var userId = _service.UserId;

        // Assert
        Assert.That(userId, Is.EqualTo("system"));
    }

    [Test]
    public void UtcNow_ReturnsCurrentUtcTime()
    {
        // Act
        var now = _service.UtcNow;

        // Assert
        Assert.That(now.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That((DateTime.UtcNow - now).TotalSeconds, Is.LessThan(1));
    }
}
```

### Background Job Context (Non-HTTP)

For operations outside HTTP request context:

```csharp
// ForgeKit.Api/Services/BackgroundJobAuditContext.cs
public class BackgroundJobAuditContext : IAuditContext
{
    private readonly string _userId;

    public BackgroundJobAuditContext(string userId = "background-job")
    {
        _userId = userId;
    }

    public string UserId => _userId;
    public string UserName => _userId;
    public DateTime UtcNow => DateTime.UtcNow;
}

// Usage in scheduled job
var auditContext = new BackgroundJobAuditContext("migration-job");
var service = new TodoService(_unitOfWork, auditContext, _softDeleteService);
```

## ForgeKit-Specific Considerations

### JWT Claim Configuration
Verify your JWT configuration uses standard claims:

```csharp
// ConfigureJwtBearerOptions.cs
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // ... other config ...
        NameClaimType = ClaimTypes.NameIdentifier,  // Standard claim type
    };
});
```

### Multiple Audit Fields
IAuditContext can also be used for other audit-related operations:

```csharp
// In domain services
public class TodoStatusHistoryService
{
    private readonly IAuditContext _auditContext;

    public TodoStatusHistory CreateAudit(string action, string details)
    {
        return new TodoStatusHistory
        {
            Action = action,
            Details = details,
            AuditedBy = _auditContext.UserId,
            AuditedAt = _auditContext.UtcNow,
            AuditedByName = _auditContext.UserName
        };
    }
}
```

## Related ADRs

- **ADR-002:** Soft-delete pattern (uses IAuditContext for DeletedBy)
- **ADR-004:** Direct DbContext usage (IUnitOfWork.SaveChangesAsync uses IAuditContext)
- **ADR-007:** Unit of Work pattern (automatically populates audit fields)

## References

- [ASP.NET Core Security Claims](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Claims Best Practices](https://tools.ietf.org/html/rfc7519)
- [.NET ClaimTypes Reference](https://docs.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes)
