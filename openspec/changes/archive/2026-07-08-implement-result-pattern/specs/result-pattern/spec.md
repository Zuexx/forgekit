# Result Pattern Specification

**Change ID:** implement-result-pattern  
**Status:** Implemented  
**Version:** 1.0  
**Last Updated:** February 2026

---

## ADDED Requirements

### Requirement: Result Pattern Handler Responses
The system SHALL provide a `Result<T>` response pattern for MediatR handlers so expected business failures are represented explicitly without throwing exceptions.

#### Scenario: Handler returns explicit failure
- **WHEN** a handler encounters an expected business failure such as a missing resource
- **THEN** it returns `Result<T>.Failure` with a machine-readable code and human-readable message
- **AND** endpoint mapping converts the failure to the appropriate HTTP response

## Overview

This specification defines the Result Pattern implementation for type-safe error handling in the Scaffold API, replacing exception-based error handling for expected business errors.

---

## Goals

### Primary Goals
1. **Type Safety** - Compiler-enforced error handling through discriminated unions
2. **Performance** - Avoid exception throwing overhead for expected errors
3. **Clarity** - Make error paths explicit in code
4. **Composability** - Enable functional composition of operations

### Non-Goals
- Replace exceptions for unexpected/exceptional errors (e.g., database connection failures)
- Modify existing middleware infrastructure
- Change validation pipeline (FluentValidation continues to work)

---

## Result Type Definition

### Type Hierarchy

```csharp
public abstract record Result<T>
{
    /// <summary>
    /// Successful operation with data
    /// </summary>
    public sealed record Success(T Data) : Result<T>;
    
    /// <summary>
    /// Failed operation with error details
    /// </summary>
    public sealed record Failure(
        string Code,           // Machine-readable error code (e.g., "USER_NOT_FOUND")
        string Message,        // Human-readable error message
        IReadOnlyDictionary<string, string[]>? Details = null  // Optional field-level errors
    ) : Result<T>;
}
```

### Type Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| **Success.Data** | `T` | ✅ Yes | The successful operation result |
| **Failure.Code** | `string` | ✅ Yes | Machine-readable error code (e.g., "RESOURCE_NOT_FOUND") |
| **Failure.Message** | `string` | ✅ Yes | Human-readable error message |
| **Failure.Details** | `IReadOnlyDictionary<string, string[]>?` | ❌ No | Field-level validation errors |

---

## Error Code Standards

### Naming Convention

```
{ENTITY}_{ERROR_TYPE}
```

**Examples:**
- `USER_NOT_FOUND` - User entity not found
- `ORDER_CONFLICT` - Order state conflict
- `INVALID_EMAIL` - Invalid email field
- `UNAUTHORIZED_ACCESS` - Permission denied

### Standard Error Codes

| Code Pattern | HTTP Status | Use Case | Example |
|--------------|-------------|----------|---------|
| `{ENTITY}_NOT_FOUND` | 404 | Resource not found | `USER_NOT_FOUND`, `ORDER_NOT_FOUND` |
| `{OPERATION}_CONFLICT` | 409 | Business rule conflict | `ORDER_CONFLICT`, `APPOINTMENT_CONFLICT` |
| `INVALID_{FIELD}` | 400 | Validation failure | `INVALID_EMAIL`, `INVALID_DATE` |
| `UNAUTHORIZED_{OPERATION}` | 403 | Permission denied | `UNAUTHORIZED_DELETE`, `UNAUTHORIZED_ACCESS` |
| `VALIDATION_ERROR` | 400 | Multiple validation errors | General validation failure |
| `INTERNAL_ERROR` | 500 | Unexpected server error | Fallback for unhandled errors |

### Error Code Registry

Location: `Api/Constants/ErrorCodes.cs`

```csharp
public static class ErrorCodes
{
    // Generic
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    
    // Resource-specific (example)
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
    public const string RESOURCE_ALREADY_EXISTS = "RESOURCE_ALREADY_EXISTS";
    
    // Add domain-specific codes as needed
}
```

---

## Handler Patterns

### Query Handler (Read Operation)

```csharp
public class GetUserByIdQueryHandler : ResultQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly AppDbContext _context;
    
    public GetUserByIdQueryHandler(
        AppDbContext context,
        ILogger<GetUserByIdQueryHandler> logger) : base(logger)
    {
        _context = context;
    }
    
    public override async Task<Result<UserDto>> HandleAsync(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Id))
            return Failure("INVALID_ID", "User ID is required");
        
        // Query
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct);
        
        // Not found check
        if (user == null)
            return Failure("USER_NOT_FOUND", $"User '{request.Id}' not found");
        
        // Map to DTO
        var dto = new UserDto(user.Id, user.Name, user.Email);
        
        // Success
        return Success(dto);
    }
}
```

### Command Handler (Write Operation)

```csharp
public class CreateUserCommandHandler : ResultCommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger) : base(logger)
    {
        _unitOfWork = unitOfWork;
    }
    
    public override async Task<Result<UserDto>> HandleAsync(
        CreateUserCommand request,
        CancellationToken ct)
    {
        // Business rule check
        var existing = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        
        if (existing != null)
            return Failure("USER_ALREADY_EXISTS", $"User with email '{request.Email}' already exists");
        
        // Create entity
        var user = new User
        {
            Id = Guid.NewGuid().ToString("N").ToLower(),
            Name = request.Name,
            Email = request.Email
        };
        
        // Save
        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync(ct);
        
        // Map to DTO
        var dto = new UserDto(user.Id, user.Name, user.Email);
        
        // Success
        return Success(dto);
    }
}
```

### Handler with Field-Level Errors

```csharp
public override async Task<Result<UserDto>> HandleAsync(
    CreateUserCommand request,
    CancellationToken ct)
{
    var errors = new Dictionary<string, string[]>();
    
    // Collect field-level errors
    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add("name", new[] { "Name is required" });
    
    if (!IsValidEmail(request.Email))
        errors.Add("email", new[] { "Email format is invalid" });
    
    // Return failure with field details
    if (errors.Any())
        return Failure("VALIDATION_ERROR", "One or more validation errors occurred", errors);
    
    // ... proceed with creation
}
```

---

## Endpoint Integration

### Minimal API Pattern

```csharp
public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("/api/v1/users")
        .WithTags("Users");

    group.MapGet("/{id}", GetUserById)
        .WithName("GetUserById")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

    group.MapPost("/", CreateUser)
        .WithName("CreateUser")
        .Produces<UserDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

    return endpoints;
}

private static async Task<IResult> GetUserById(
    string id,
    IMediator mediator,
    CancellationToken ct)
{
    var query = new GetUserByIdQuery(id);
    var result = await mediator.Send(query, ct);

    return result switch
    {
        Result<UserDto>.Success success => Results.Ok(success.Data),
        
        Result<UserDto>.Failure failure => failure.Code switch
        {
            "USER_NOT_FOUND" => Results.NotFound(new { error = failure.Code, message = failure.Message }),
            "INVALID_ID" => Results.BadRequest(new { error = failure.Code, message = failure.Message }),
            _ => Results.Problem(statusCode: 500, title: "Internal Server Error")
        },
        
        _ => throw new InvalidOperationException("Unexpected result type")
    };
}

private static async Task<IResult> CreateUser(
    CreateUserCommand command,
    IMediator mediator,
    CancellationToken ct)
{
    var result = await mediator.Send(command, ct);

    return result switch
    {
        Result<UserDto>.Success success => 
            Results.Created($"/api/v1/users/{success.Data.Id}", success.Data),
        
        Result<UserDto>.Failure failure => failure.Code switch
        {
            "USER_ALREADY_EXISTS" => Results.Conflict(new { error = failure.Code, message = failure.Message }),
            "VALIDATION_ERROR" => Results.BadRequest(new 
            { 
                error = failure.Code, 
                message = failure.Message,
                errors = failure.Details 
            }),
            _ => Results.BadRequest(new { error = failure.Code, message = failure.Message })
        },
        
        _ => throw new InvalidOperationException("Unexpected result type")
    };
}
```

---

## Extension Methods

### Map Operation (Transform Success Data)

```csharp
public static Result<TNext> Map<T, TNext>(
    this Result<T> result,
    Func<T, TNext> mapper)
{
    return result switch
    {
        Result<T>.Success success => Result<TNext>.Success(mapper(success.Data)),
        Result<T>.Failure failure => Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
        _ => throw new InvalidOperationException()
    };
}

// Usage
var userResult = await GetUserById(id);
var userNameResult = userResult.Map(user => user.Name);
```

### Bind Operation (Chain Results)

```csharp
public static Result<TNext> Bind<T, TNext>(
    this Result<T> result,
    Func<T, Result<TNext>> binder)
{
    return result switch
    {
        Result<T>.Success success => binder(success.Data),
        Result<T>.Failure failure => Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
        _ => throw new InvalidOperationException()
    };
}

// Usage
var result = await GetUserById(id)
    .Bind(user => UpdateUserStatus(user.Id, "Active"));
```

### BindAsync Operation (Async Chaining)

```csharp
public static async Task<Result<TNext>> BindAsync<T, TNext>(
    this Task<Result<T>> resultTask,
    Func<T, Task<Result<TNext>>> binder)
{
    var result = await resultTask;
    
    return result switch
    {
        Result<T>.Success success => await binder(success.Data),
        Result<T>.Failure failure => Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
        _ => throw new InvalidOperationException()
    };
}

// Usage
var result = await GetUserByIdAsync(id)
    .BindAsync(user => UpdateUserStatusAsync(user.Id, "Active"));
```

### Match Operation (Pattern Matching with Callbacks)

```csharp
public static TResult Match<T, TResult>(
    this Result<T> result,
    Func<T, TResult> onSuccess,
    Func<string, string, IReadOnlyDictionary<string, string[]>?, TResult> onFailure)
{
    return result switch
    {
        Result<T>.Success success => onSuccess(success.Data),
        Result<T>.Failure failure => onFailure(failure.Code, failure.Message, failure.Details),
        _ => throw new InvalidOperationException()
    };
}

// Usage
var httpResult = result.Match(
    user => Results.Ok(user),
    (code, message, details) => Results.NotFound(new { error = code, message })
);
```

---

## Integration with Existing Patterns

### With FluentValidation

FluentValidation continues to work normally. `ValidationBehavior` throws `ValidationAppException` which is caught by `ExceptionHandlingMiddleware`.

**Result pattern is used AFTER validation passes:**

```
Request
  ↓
ValidationBehavior (FluentValidation)
  ├─ Validation fails → throw ValidationAppException (middleware catches)
  └─ Validation passes
       ↓
Handler (Result Pattern)
  ├─ Business rule fails → return Result.Failure (endpoint handles)
  └─ Success → return Result.Success (endpoint handles)
```

### With Exception Handling Middleware

Middleware continues to catch exceptions. Result pattern does NOT change middleware behavior.

**Exception for unexpected errors:**
```csharp
// Unexpected error → throw exception (middleware catches)
if (dbContext == null)
    throw new InvalidOperationException("DbContext not initialized");

// Expected error → return failure (endpoint handles)
if (user == null)
    return Failure("USER_NOT_FOUND", "User not found");
```

### With Audit Trail & Unit of Work

Result pattern works seamlessly with audit context and UnitOfWork.

```csharp
public override async Task<Result<UserDto>> HandleAsync(
    CreateUserCommand request,
    CancellationToken ct)
{
    var user = new User { /* ... */ };
    
    _unitOfWork.Users.Add(user);
    await _unitOfWork.SaveChangesAsync(ct);  // Audit fields populated automatically
    
    return Success(new UserDto(user.Id, user.Name));
}
```

---

## Testing

### Testing Handlers

```csharp
[Fact]
public async Task HandleAsync_UserExists_ReturnsSuccess()
{
    // Arrange
    var handler = new GetUserByIdQueryHandler(_context, _logger);
    var query = new GetUserByIdQuery("user123");

    // Act
    var result = await handler.HandleAsync(query, CancellationToken.None);

    // Assert
    result.Should().BeOfType<Result<UserDto>.Success>();
    var success = (Result<UserDto>.Success)result;
    success.Data.Id.Should().Be("user123");
}

[Fact]
public async Task HandleAsync_UserNotFound_ReturnsFailure()
{
    // Arrange
    var handler = new GetUserByIdQueryHandler(_context, _logger);
    var query = new GetUserByIdQuery("nonexistent");

    // Act
    var result = await handler.HandleAsync(query, CancellationToken.None);

    // Assert
    result.Should().BeOfType<Result<UserDto>.Failure>();
    var failure = (Result<UserDto>.Failure)result;
    failure.Code.Should().Be("USER_NOT_FOUND");
}
```

### Testing Endpoints

```csharp
[Fact]
public async Task GetUserById_UserExists_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/users/user123");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var user = await response.Content.ReadFromJsonAsync<UserDto>();
    user.Should().NotBeNull();
    user!.Id.Should().Be("user123");
}

[Fact]
public async Task GetUserById_UserNotFound_ReturnsNotFound()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/users/nonexistent");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    error.Should().NotBeNull();
    error!.Error.Should().Be("USER_NOT_FOUND");
}
```

---

## Performance Considerations

### Exception vs Result Pattern

| Operation | Exception-Based | Result-Based | Improvement |
|-----------|----------------|--------------|-------------|
| Success path | ~50 ns | ~50 ns | No change |
| Expected error | ~5,000 ns (throw/catch) | ~50 ns | **~100x faster** |
| Unexpected error | ~5,000 ns | ~5,000 ns | No change |

**Key Insight:** Result pattern avoids exception throwing overhead for **expected** errors.

### Memory Allocation

```csharp
// Result pattern - minimal allocation
return Success(data);  // Single object allocation

// Exception pattern - significant allocation
throw new NotFoundException(...);  // Exception + stack trace + message
```

---

## Migration Strategy

### Incremental Adoption

1. **New features** - Use Result pattern from day one
2. **Existing features** - Migrate on modification (not required immediately)
3. **Hot paths** - Prioritize high-traffic endpoints for performance gains

### Backward Compatibility

- Exception handling middleware continues to work
- Existing exception-based handlers continue to work
- Can mix Result and Exception patterns during migration

---

## Best Practices

### ✅ DO

1. **Return Result.Failure for expected errors**
   ```csharp
   if (user == null)
       return Failure("USER_NOT_FOUND", "User not found");
   ```

2. **Use standard error codes** (defined in ErrorCodes.cs)
   ```csharp
   return Failure(ErrorCodes.RESOURCE_NOT_FOUND, $"Resource '{id}' not found");
   ```

3. **Include field-level errors for validation**
   ```csharp
   return Failure("VALIDATION_ERROR", "Validation failed", fieldErrors);
   ```

4. **Use extension methods for composition**
   ```csharp
   return await GetUserAsync(id)
       .BindAsync(user => UpdateStatusAsync(user.Id, "Active"));
   ```

### ❌ DON'T

1. **Don't use Result for unexpected errors**
   ```csharp
   // Bad
   if (dbContext == null)
       return Failure("DB_ERROR", "Database not initialized");
   
   // Good
   if (dbContext == null)
       throw new InvalidOperationException("DbContext not initialized");
   ```

2. **Don't throw exceptions for expected errors**
   ```csharp
   // Bad
   if (user == null)
       throw new NotFoundException("User not found");
   
   // Good
   if (user == null)
       return Failure("USER_NOT_FOUND", "User not found");
   ```

3. **Don't create ad-hoc error codes**
   ```csharp
   // Bad
   return Failure("oops", "something went wrong");
   
   // Good
   return Failure(ErrorCodes.INTERNAL_ERROR, "An unexpected error occurred");
   ```

---

## Summary

### Key Benefits

- ✅ **Type Safety** - Compiler enforces error handling
- ✅ **Performance** - Avoid exception overhead (~100x faster for expected errors)
- ✅ **Clarity** - Error paths explicit in code
- ✅ **Composability** - Functional composition via Map/Bind
- ✅ **Backward Compatible** - Works alongside existing patterns

### Implementation Files

| File | Purpose |
|------|---------|
| `Api/Models/Result.cs` | Result<T> type definition |
| `Api/Extensions/ResultExtensions.cs` | Extension methods (Map, Bind, Match) |
| `Api/Handlers/ResultQueryHandler.cs` | Base handler for queries |
| `Api/Handlers/ResultCommandHandler.cs` | Base handler for commands |
| `Api/Constants/ErrorCodes.cs` | Error code constants |

---

**Related Documents:**
- [RESULT_PATTERN_GUIDE.md](../../../docs/RESULT_PATTERN_GUIDE.md) - User guide
- [design.md](../design.md) - Design document
- [ADR-006: Error Response Standardization](../../../docs/adr/006-error-response-standardization.md)

**Specification Version:** 1.0  
**Date:** February 2026  
**Status:** Implemented & Verified
