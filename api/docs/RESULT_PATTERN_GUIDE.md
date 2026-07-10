# Result Pattern Implementation Guide

## Overview

This guide documents the **Result Pattern** (Railway-Oriented Programming) for handling success and failure responses in MediatR handlers. The Result pattern provides an alternative to exception-based error handling for **expected business errors**, allowing type-safe error handling throughout the application.

The Result pattern is **universal and domain-agnostic**, applicable to all handler types and entity domains.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Result Pattern Basics](#result-pattern-basics)
3. [When to Use Result vs Exception](#when-to-use-result-vs-exception)
4. [Implementation Patterns](#implementation-patterns)
5. [Error Code Conventions](#error-code-conventions)
6. [Handler Implementation Guide](#handler-implementation-guide)
7. [Minimal API Integration](#minimal-api-integration)
8. [Best Practices](#best-practices)
9. [Testing Patterns](#testing-patterns)
10. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Basic Result Pattern Flow

```csharp
// 1. Handler returns Result<T>
public class GetResourceByIdQueryHandler : ResultQueryHandler<GetResourceByIdQuery, ResourceDto>
{
    public override async Task<Result<ResourceDto>> Handle(
        GetResourceByIdQuery request, 
        CancellationToken ct)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Id))
            return Failure("INVALID_ID", "Resource ID is required");
        
        // Database operation
        var resource = await _context.Resources.FirstOrDefaultAsync(
            r => r.Id == request.Id, ct);
        
        if (resource == null)
            return Failure("RESOURCE_NOT_FOUND", $"Resource '{request.Id}' not found");
        
        // Success
        return Success(_mapper.Map<ResourceDto>(resource));
    }
}

// 2. Endpoint handles Result<T>
app.MapGet("/api/resources/{id}", GetResourceById)
    .Produces<ResourceDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest);

async Task<IResult> GetResourceById(string id, IMediator mediator)
{
    var result = await mediator.Send(new GetResourceByIdQuery(id));
    
    return result switch
    {
        Result<ResourceDto>.Success success => 
            Results.Ok(success.Data),
        
        Result<ResourceDto>.Failure failure => 
            Results.Problem(
                detail: failure.Message,
                title: failure.Code,
                statusCode: MapErrorCodeToHttpStatus(failure.Code)
            ),
        
        _ => Results.StatusCode(500)
    };
}
```

---

## Result Pattern Basics

### Result<T> Type Structure

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    
    public sealed record Failure(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null
    ) : Result<T>;
}
```

**Key Characteristics:**
- **Abstract record** with two sealed derived records
- **Discriminated union** - compiler ensures all cases handled
- **Immutable** - records provide value semantics
- **Type-safe** - pattern matching is compile-time verified

### Success Case

The `Success` record contains the operation result:

```csharp
var result = new Result<UserDto>.Success(userData);
// Access data
var data = result.Data;  // Type-safe access
```

### Failure Case

The `Failure` record contains error information:

```csharp
var result = new Result<UserDto>.Failure(
    Code: "USER_NOT_FOUND",
    Message: "User with ID 123 not found",
    Details: null  // Optional field-level errors
);

// Access error info
var code = result.Code;
var message = result.Message;
var details = result.Details;  // Dictionary<string, string[]>
```

### Extension Methods

The Result type provides several extension methods for composition:

#### Map - Transform Success Data

```csharp
var result = new Result<User>.Success(user);

// Transform the data
var dtoResult = result.Map(u => new UserDto { Id = u.Id, Name = u.Name });
// dtoResult is Result<UserDto>.Success
```

#### Bind - Chain Operations (Monadic Composition)

```csharp
public Result<UserDto> GetUserWithDetails(string userId)
{
    // First operation: Get user
    var userResult = GetUser(userId);  // Returns Result<User>
    
    // Chain: If success, load details; if failure, propagate
    return userResult.Bind(user => LoadUserDetails(user));
}
```

#### BindAsync - Async Composition

```csharp
public async Task<Result<UserDto>> GetUserWithDetailsAsync(string userId)
{
    // Chain async operations
    var result = await GetUserAsync(userId)
        .BindAsync(user => LoadUserDetailsAsync(user));
    
    return result;
}
```

#### Match - Pattern Matching

```csharp
var message = result.Match(
    onSuccess: data => $"Success: {data.Name}",
    onFailure: (code, msg, details) => $"Error: {code} - {msg}"
);
```

#### OnSuccess / OnFailure - Side Effects

```csharp
var result = GetUser(userId)
    .OnSuccess(user => _logger.LogInformation($"User loaded: {user.Id}"))
    .OnFailure((code, msg) => _logger.LogWarning($"Failed: {code}"));
```

---

## When to Use Result vs Exception

### Use Result Pattern For:

✅ **Expected business logic outcomes**
- Validation failures with clear error codes
- Resource not found scenarios
- State conflicts (e.g., "already exists")
- Authorization failures (forbidden operations)
- Business rule violations

**Characteristics:**
- Part of normal operation
- Caller expects and handles these cases
- No exceptional behavior
- Performance-critical paths (no stack trace overhead)

**Example:**
```csharp
// "User not found" is an expected outcome of looking up a user
if (user == null)
    return Failure("USER_NOT_FOUND", "User not found");
```

### Use Exceptions For:

❌ **Truly unexpected errors**
- Database connection failures
- Configuration errors
- Null reference violations (programmer error)
- Out of memory conditions
- Unrecoverable system failures

**Characteristics:**
- Not expected in normal operation
- Indicates something is seriously wrong
- Recovery is not expected in normal flow
- Needs detailed stack trace for debugging

**Example:**
```csharp
// Database connection suddenly fails - this is unexpected
try 
{
    var user = await _context.Users.FindAsync(id);
}
catch (PostgresException ex)
{
    // This bubbles up - it's unexpected and needs investigation
    throw new ApplicationException("Database connection failed", ex);
}
```

### Decision Flowchart

```
Is this an expected outcome of normal operation?
├─ YES → Use Result Pattern ✅
│   └─ Return Result.Failure() in handler
│       └─ Handle in controller with pattern matching
│
└─ NO → Use Exceptions ❌
    └─ Throw exception in handler
        └─ Caught by middleware, returns 500
```

---

## Implementation Patterns

### Pattern 1: Using Base Handler Classes

Base handler classes enforce the Result pattern:

```csharp
public class GetResourceByIdQueryHandler : ResultQueryHandler<GetResourceByIdQuery, ResourceDto>
{
    public override async Task<Result<ResourceDto>> Handle(
        GetResourceByIdQuery request,
        CancellationToken ct)
    {
        // Base class provides:
        // - Success(T) helper
        // - Failure(code, message) helper
        // - Standard error codes
        
        var resource = await _repository.GetAsync(request.Id, ct);
        if (resource == null)
            return Failure(StandardErrorCodes.NotFound("Resource"), "Resource not found");
        
        return Success(_mapper.Map<ResourceDto>(resource));
    }
}
```

**Available Base Classes:**
- `ResultQueryHandler<TRequest, TResponse>` - For queries
- `ResultCommandHandler<TRequest, TResponse>` - For commands

### Pattern 2: Error Code Consistency

Always use consistent error codes following universal conventions:

```csharp
// Standard codes applicable to any domain
"ENTITY_NOT_FOUND"      // Any resource not found (404)
"OPERATION_CONFLICT"    // Any state conflict (409)
"INVALID_FIELD"         // Input validation failure (400)
"UNAUTHORIZED_ACTION"   // Permission denied (403)
"INTERNAL_ERROR"        // Unexpected system error (500)
```

**Example:**
```csharp
// These codes work universally across all domains:
return Failure("USER_NOT_FOUND", "User not found");           // 404
return Failure("VISIT_CONFLICT", "MR already has visit");     // 409
return Failure("INVALID_DATE", "Date must be future");        // 400
```

### Pattern 3: Field-Level Validation Details

Include field-level errors in the Details dictionary:

```csharp
var errors = new Dictionary<string, string[]>
{
    { "visitDate", new[] { "Visit date must be at least 5 days in advance" } },
    { "mrId", new[] { "Medical Representative does not exist" } }
};

return Failure("VALIDATION_FAILED", "One or more validation errors occurred", errors);
```

---

## Error Code Conventions

### Universal Error Code Patterns

These patterns apply universally to **any domain or entity**:

| Pattern | HTTP Status | Meaning | Example |
|---------|------------|---------|---------|
| `{ENTITY}_NOT_FOUND` | 404 | Resource doesn't exist | `USER_NOT_FOUND`, `VISIT_NOT_FOUND` |
| `{OPERATION}_CONFLICT` | 409 | Business rule conflict | `CREATE_CONFLICT`, `SCHEDULE_CONFLICT` |
| `INVALID_{FIELD}` | 400 | Input validation failed | `INVALID_DATE`, `INVALID_EMAIL` |
| `UNAUTHORIZED_{OPERATION}` | 403 | Permission denied | `UNAUTHORIZED_DELETE`, `UNAUTHORIZED_APPROVE` |
| `INTERNAL_ERROR` | 500 | Unexpected error | System failure |

### Mapping Error Codes to HTTP Status

Use the `ResultErrorCodeMapper` extension method:

```csharp
int statusCode = failure.Code switch
{
    var code when code.EndsWith("_NOT_FOUND") => StatusCodes.Status404NotFound,
    var code when code.EndsWith("_CONFLICT") => StatusCodes.Status409Conflict,
    var code when code.StartsWith("INVALID_") => StatusCodes.Status400BadRequest,
    var code when code.StartsWith("UNAUTHORIZED_") => StatusCodes.Status403Forbidden,
    _ => StatusCodes.Status500InternalServerError
};
```

---

## Handler Implementation Guide

### Query Handler Template

```csharp
public class GetResourceByIdQueryHandler : ResultQueryHandler<GetResourceByIdQuery, ResourceDto>
{
    private readonly DbContext _context;
    private readonly IMapper _mapper;
    
    public GetResourceByIdQueryHandler(DbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public override async Task<Result<ResourceDto>> Handle(
        GetResourceByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate input
        if (string.IsNullOrWhiteSpace(request.Id))
            return Failure("INVALID_ID", "Resource ID is required");
        
        // Step 2: Query database
        var resource = await _context.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        
        // Step 3: Handle not found
        if (resource == null)
            return Failure("RESOURCE_NOT_FOUND", $"Resource with ID '{request.Id}' was not found");
        
        // Step 4: Return success
        return Success(_mapper.Map<ResourceDto>(resource));
    }
}
```

### Command Handler Template

```csharp
public class CreateResourceCommandHandler : ResultCommandHandler<CreateResourceCommand, ResourceDto>
{
    private readonly DbContext _context;
    private readonly IMapper _mapper;
    
    public CreateResourceCommandHandler(DbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public override async Task<Result<ResourceDto>> Handle(
        CreateResourceCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            return Failure("INVALID_NAME", "Resource name is required");
        
        // Step 2: Check business constraints
        var existing = await _context.Resources
            .FirstOrDefaultAsync(r => r.Name == request.Name, cancellationToken);
        
        if (existing != null)
            return Failure("RESOURCE_CONFLICT", "Resource with this name already exists");
        
        // Step 3: Create entity
        var resource = new Resource
        {
            Id = Guid.NewGuid().ToString("N").ToLower(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };
        
        // Step 4: Persist
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Step 5: Return success
        return Success(_mapper.Map<ResourceDto>(resource));
    }
}
```

---

## Minimal API Integration

### Setting Up Result Handling

Register result conversion in Minimal API:

```csharp
// In Program.cs or module configuration
app.MapGet("/api/resources/{id}", GetResourceById)
    .Produces<ResourceDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("GetResourceById")
    .WithOpenApi();

async Task<IResult> GetResourceById(string id, IMediator mediator)
{
    var result = await mediator.Send(new GetResourceByIdQuery(id));
    
    return result switch
    {
        Result<ResourceDto>.Success success =>
            Results.Ok(success.Data),
        
        Result<ResourceDto>.Failure failure =>
            Results.Problem(
                detail: failure.Message,
                title: failure.Code,
                statusCode: MapErrorCodeToHttpStatus(failure.Code),
                extensions: failure.Details != null ?
                    new Dictionary<string, object?> { { "errors", failure.Details } } :
                    null
            ),
        
        _ => Results.StatusCode(500)
    };
}

static int MapErrorCodeToHttpStatus(string code) => code switch
{
    var c when c.EndsWith("_NOT_FOUND") => StatusCodes.Status404NotFound,
    var c when c.EndsWith("_CONFLICT") => StatusCodes.Status409Conflict,
    var c when c.StartsWith("INVALID_") => StatusCodes.Status400BadRequest,
    var c when c.StartsWith("UNAUTHORIZED_") => StatusCodes.Status403Forbidden,
    _ => StatusCodes.Status500InternalServerError
};
```

### Using the IModule Pattern

Organize endpoints using the `IModule` pattern:

```csharp
public class ResourceModule : IModule
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/resources")
            .WithTags("Resources");
        
        group.MapGet("/{id}", GetById)
            .WithName("GetResourceById");
        
        group.MapPost("", Create)
            .WithName("CreateResource");
        
        group.MapPut("/{id}", Update)
            .WithName("UpdateResource");
    }
    
    private static async Task<IResult> GetById(string id, IMediator mediator)
    {
        var result = await mediator.Send(new GetResourceByIdQuery(id));
        return MapResult(result, StatusCodes.Status200OK);
    }
    
    private static async Task<IResult> Create(CreateResourceRequest request, IMediator mediator)
    {
        var result = await mediator.Send(new CreateResourceCommand(request.Name));
        return MapResult(result, StatusCodes.Status201Created);
    }
    
    private static IResult MapResult<T>(Result<T> result, int successStatus) => result switch
    {
        Result<T>.Success success => 
            Results.StatusCode(successStatus, success.Data),
        
        Result<T>.Failure failure =>
            Results.Problem(
                detail: failure.Message,
                title: failure.Code,
                statusCode: MapErrorCodeToHttpStatus(failure.Code),
                extensions: failure.Details != null ?
                    new Dictionary<string, object?> { { "errors", failure.Details } } :
                    null
            ),
        
        _ => Results.StatusCode(500)
    };
    
    private static int MapErrorCodeToHttpStatus(string code) => code switch
    {
        var c when c.EndsWith("_NOT_FOUND") => StatusCodes.Status404NotFound,
        var c when c.EndsWith("_CONFLICT") => StatusCodes.Status409Conflict,
        var c when c.StartsWith("INVALID_") => StatusCodes.Status400BadRequest,
        var c when c.StartsWith("UNAUTHORIZED_") => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}

// Register in Program.cs
services.AddModules(typeof(Program));
```

---

## Best Practices

### 1. Clear Error Messages

Write error messages that help API consumers understand what went wrong:

```csharp
// ❌ Bad: Too vague
return Failure("ERROR", "Operation failed");

// ✅ Good: Specific and actionable
return Failure("INVALID_DATE", "Visit date must be at least 5 days in the future");
```

### 2. Consistent Error Code Naming

Use a consistent naming convention based on entity and operation:

```csharp
// ✅ Consistent pattern: {ENTITY}_{OPERATION}
"USER_NOT_FOUND"
"VISIT_CONFLICT"
"SCHEDULE_CONFLICT"
"INVALID_DATE"
```

### 3. Field-Level Validation Details

Include field information when multiple validation failures occur:

```csharp
var errors = new Dictionary<string, string[]>
{
    { "visitDate", new[] { "Must be at least 5 days in future" } },
    { "mrId", new[] { "Medical Representative does not exist" } },
    { "hcoId", new[] { "Healthcare Organization does not exist" } }
};

return Failure("VALIDATION_ERRORS", "Multiple validation errors", errors);
```

### 4. No Sensitive Information

Never include sensitive data in error responses:

```csharp
// ❌ Bad: Exposes internal details
return Failure("DB_ERROR", "Couldn't connect to 'prod_db.company.com':5432");

// ✅ Good: Generic message
return Failure("INTERNAL_ERROR", "An unexpected error occurred");
```

### 5. Leverage Type Safety

Use pattern matching to ensure all cases are handled:

```csharp
// ✅ Compiler ensures both cases handled
var response = result switch
{
    Result<UserDto>.Success success => Ok(success.Data),
    Result<UserDto>.Failure failure => HandleFailure(failure),
    _ => throw new InvalidOperationException()  // Never reached
};

// ❌ Avoid: Old try-catch style
try 
{
    var data = handler.Handle(request);
}
catch (Exception ex) 
{
    // Easy to miss cases
}
```

---

## Testing Patterns

### Unit Test: Success Case

```csharp
[Fact]
public async Task Handle_WhenValidId_ReturnsSuccess()
{
    // Arrange
    var handler = new GetResourceByIdQueryHandler(_mockContext, _mockMapper);
    var query = new GetResourceByIdQuery("resource-123");
    var resource = new Resource { Id = "resource-123", Name = "Test" };
    
    _mockContext.Resources
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Resource, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(resource);
    
    _mockMapper.Setup(m => m.Map<ResourceDto>(resource))
        .Returns(new ResourceDto { Id = "resource-123", Name = "Test" });
    
    // Act
    var result = await handler.Handle(query, CancellationToken.None);
    
    // Assert
    result.ShouldBeOfType<Result<ResourceDto>.Success>();
    ((Result<ResourceDto>.Success)result).Data.Id.ShouldBe("resource-123");
}
```

**Note:** This project uses **Shouldly** for assertions.

### Unit Test: Failure Case

```csharp
[Fact]
public async Task Handle_WhenResourceNotFound_ReturnsFailure()
{
    // Arrange
    var handler = new GetResourceByIdQueryHandler(_mockContext, _mockMapper);
    var query = new GetResourceByIdQuery("non-existent");
    
    _mockContext.Resources
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Resource, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Resource)null);
    
    // Act
    var result = await handler.Handle(query, CancellationToken.None);
    
    // Assert
    result.ShouldBeOfType<Result<ResourceDto>.Failure>();
    var failure = (Result<ResourceDto>.Failure)result;
    failure.Code.ShouldBe("RESOURCE_NOT_FOUND");
    failure.Message.ShouldContain("non-existent");
}
```

**Note:** This project uses **Shouldly** for assertions.

### Integration Test: HTTP Response

```csharp
[Fact]
public async Task GetById_WhenResourceNotFound_Returns404()
{
    // Act
    var response = await _client.GetAsync("/api/resources/non-existent");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var content = await response.Content.ReadAsAsync<ProblemDetails>();
    content.Title.Should().Be("RESOURCE_NOT_FOUND");
}
```

---

## Troubleshooting

### Issue: Compiler complains about pattern matching

**Problem:**
```
error CS8505: Pattern is not exhaustive, not all cases matched
```

**Solution:** Ensure all Result cases are handled:

```csharp
// ✅ Correct: All cases handled
var response = result switch
{
    Result<UserDto>.Success success => ...,
    Result<UserDto>.Failure failure => ...,
};
```

### Issue: Failure details not appearing in response

**Problem:** Field-level error details are null.

**Solution:** Provide Details dictionary when creating Failure:

```csharp
// ✅ Correct
var errors = new Dictionary<string, string[]>
{
    { "email", new[] { "Invalid format" } }
};
return Failure("INVALID_EMAIL", "Email validation failed", errors);
```

### Issue: Performance concerns with Result type

**Problem:** Concerned about performance overhead.

**Solution:** Result<T> is a record (value type semantics) with minimal overhead:
- No exception stack trace generation
- No GC pressure from thrown exceptions
- Faster than exception-based error handling for expected errors

### Issue: How to convert existing exception-based handler

**Problem:** Have existing handlers throwing exceptions, want to migrate.

**Solution:** Gradual migration - no breaking changes needed:

```csharp
// Old style (still works)
public class OldHandler : IRequestHandler<Query, Dto>
{
    public async Task<Dto> Handle(Query request, CancellationToken ct)
    {
        if (resource == null)
            throw new NotFoundException("Not found");  // Still works
        return dto;
    }
}

// New style (use going forward)
public class NewHandler : ResultQueryHandler<Query, Dto>
{
    public override async Task<Result<Dto>> Handle(Query request, CancellationToken ct)
    {
        if (resource == null)
            return Failure("NOT_FOUND", "Not found");  // Preferred
        return Success(dto);
    }
}
```

---

## Summary

The Result Pattern provides:

✅ **Type Safety** - Compiler ensures all cases handled  
✅ **Performance** - No exception overhead for expected errors  
✅ **Clarity** - Error handling is explicit and visible  
✅ **Composability** - Monadic operations for chaining logic  
✅ **Testability** - Easy to test both success and failure paths  

**Key Takeaway:** Use Result Pattern for **expected business outcomes** and exceptions for **truly unexpected errors**. Both patterns coexist peacefully in the same codebase.

---

## Quick Reference

### Base Class Helpers

```csharp
// In ResultQueryHandler or ResultCommandHandler
Success(data)                    // Create success result
Failure(code, message)           // Create failure result
Failure(code, message, details)  // Include field errors
```

### Extension Methods

```csharp
result.Map(x => transform(x))                    // Transform data
result.Bind(x => operation(x))                   // Chain operations
result.BindAsync(x => asyncOp(x))               // Async composition
result.Match(onSuccess, onFailure)              // Pattern matching
result.OnSuccess(action)                        // Side effects
result.OnFailure((code, msg) => action())       // Error handling
```

### HTTP Status Mapping

```
{ENTITY}_NOT_FOUND          → 404
{OPERATION}_CONFLICT        → 409
INVALID_{FIELD}             → 400
UNAUTHORIZED_{OPERATION}    → 403
INTERNAL_ERROR              → 500
```

---

## Related Documentation

- [Exception Handling Guide](./EXCEPTION_HANDLING_GUIDE.md) - When to use exceptions
- [API Errors](./API_ERRORS.md) - Error response format
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Pattern background
