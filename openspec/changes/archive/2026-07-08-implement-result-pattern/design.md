# Design: Implement Result Pattern for Handler Responses

## Architecture Overview

### Result Type Hierarchy

```
Result<T>
├── Success(T Data)
└── Failure(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details)
```

### Error Code Mapping

Result failures map to HTTP status codes using a universal pattern applicable to any domain:

| Error Code Pattern | HTTP Status | Use Case |
|---|---|---|
| `{ENTITY}_NOT_FOUND` | 404 | Any resource not found (universal) |
| `{OPERATION}_CONFLICT` | 409 | Any business rule conflict (universal) |
| `INVALID_{FIELD}` | 400 | Input validation failures (universal) |
| `UNAUTHORIZED_{OPERATION}` | 403 | Business rule authorization failure (universal) |
| `INTERNAL_ERROR` | 500 | Unexpected error |

Examples: `USER_NOT_FOUND`, `VISIT_CONFLICT`, `INVALID_DATE`, `UNAUTHORIZED_CANCEL`

### Handler Pattern

```
Request
  ↓
Handler Logic
  ├─ Validation → Result.Failure (expected, no exceptions thrown)
  ├─ Business Rules → Result.Failure (expected, no exceptions thrown)
  ├─ Execution → Result.Success(data)
  └─ Unexpected Error → throw Exception (caught by middleware)
  ↓
Controller
  ├─ Success case → HTTP 200/201
  └─ Failure case → HTTP 4xx/5xx (determined by error code)
```

This pattern is universal and applies to all domains, entities, and operations.

### Key Components

1. **Result<T> Type**
   - Generic discriminated union
   - Sealed record types for pattern matching
   - Immutable and thread-safe

2. **Extension Methods**
   - `Map<TNext>()` - Transform success data
   - `Bind<TNext>()` - Chain results (monadic bind)
   - `BindAsync<TNext>()` - Async composition
   - `Match<TResult>()` - Pattern match with callbacks
   - `OnSuccess()` / `OnFailure()` - Execute callbacks

3. **Handlers**
   - Return `Result<T>` instead of `T`
   - Return `Result.Failure()` for expected errors
   - Return `Result.Success()` for success cases
   - Throw exceptions for truly unexpected errors

4. **Controllers**
   - Pattern match on Result<T>
   - Map Success → HTTP 200/201
   - Map Failure → HTTP 4xx/5xx
   - Use error code to determine status

### Integration Points

#### With Validation
- FluentValidation can return Result.Failure
- Combine multiple validation failures
- Field-level error details in Details dictionary

#### With Middleware
- Middleware catches exceptions only
- Controllers handle Result failures
- No middleware changes for Result pattern
- Exception middleware remains unchanged

#### With Mapping
- AutoMapper works normally
- Result wraps mapped data
- No special integration needed

## Implementation Details

### Result<T> Implementation

```csharp
public abstract record Result<T>
{
    /// <summary>
    /// Represents successful operation with data
    /// </summary>
    public sealed record Success(T Data) : Result<T>;
    
    /// <summary>
    /// Represents failed operation with error information
    /// </summary>
    public sealed record Failure(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null
    ) : Result<T>;
    
    // Extensions go here
}
```

### Extension Methods

```csharp
// Transform success value
public static Result<TNext> Map<T, TNext>(
    this Result<T> result,
    Func<T, TNext> map) { ... }

// Monadic bind - chain operations
public static Result<TNext> Bind<T, TNext>(
    this Result<T> result,
    Func<T, Result<TNext>> bind) { ... }

// Async composition
public static Task<Result<TNext>> BindAsync<T, TNext>(
    this Result<T> result,
    Func<T, Task<Result<TNext>>> bind) { ... }

// Pattern matching
public static TResult Match<T, TResult>(
    this Result<T> result,
    Func<T, TResult> onSuccess,
    Func<string, string, IReadOnlyDictionary<string, string[]>?, TResult> onFailure) { ... }

// Execute side effects
public static Result<T> OnSuccess<T>(
    this Result<T> result,
    Action<T> action) { ... }

public static Result<T> OnFailure<T>(
    this Result<T> result,
    Action<string, string> action) { ... }

// Convert to exception (for edge cases)
public static T GetValueOrThrow<T>(this Result<T> result) { ... }
```

### Handler Template

```csharp
// Generic template applicable to any domain/entity
public class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, Result<ResourceDto>>
{
    private readonly DbContext _context;
    private readonly IMapper _mapper;
    
    public async Task<Result<ResourceDto>> Handle(
        GetResourceByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Id))
            return new Result<ResourceDto>.Failure(
                Code: "INVALID_ID",
                Message: "Resource ID is required"
            );
        
        // Query database
        var resource = await _context.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        
        if (resource == null)
            return new Result<ResourceDto>.Failure(
                Code: "RESOURCE_NOT_FOUND",
                Message: $"Resource with ID '{request.Id}' was not found"
            );
        
        // Return success
        return new Result<ResourceDto>.Success(_mapper.Map<ResourceDto>(resource));
    }
}
```

### Controller Template

```csharp
// Generic template for Minimal API endpoint using Result Pattern
app.MapGet("/api/resources/{id}", GetResourceById)
    .WithName("GetResourceById")
    .WithOpenApi()
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
                statusCode: MapErrorCodeToHttpStatus(failure.Code),
                extensions: failure.Details != null ? 
                    new Dictionary<string, object?> { { "errors", failure.Details } } : 
                    null
            ),
        
        _ => Results.StatusCode(500)
    };
}
```

## Behavioral Specifications

### Success Case
1. Handler executes business logic
2. Returns `Result.Success(data)`
3. Controller receives Success case
4. Maps to HTTP 200/201 with data

### Expected Failure Case
1. Handler validates preconditions
2. Returns `Result.Failure(code, message)`
3. Controller receives Failure case
4. Maps to appropriate HTTP 4xx status
5. Returns problem response

### Unexpected Error Case
1. Handler throws exception
2. Middleware catches exception
3. Logs and returns HTTP 500
4. No Result involved

## Coexistence with Exceptions

### When to Use Result Pattern
- Expected business logic outcomes
- Validation failures with clear error codes
- State conflicts and authorization failures
- Business rule violations
- Any scenario where the error is part of the normal operation contract

### When to Use Exceptions
- Truly unexpected errors (system failures)
- Database connection errors
- Configuration errors
- Programming errors
- Any scenario where recovery is not expected in normal operation

### Migration Strategy
1. **New handlers:** Use Result pattern by default
2. **Existing handlers:** Keep exception-based approach (gradual migration)
3. **High-traffic handlers:** Migrate gradually as time permits
4. **Optional:** Consider migration path when refactoring existing code

### No Breaking Changes
- Result pattern is purely additive
- Existing exception-based handlers continue to work unchanged
- Can coexist in the same codebase indefinitely
- Developers choose which pattern fits their scenario

## Testing Strategy

### Unit Testing Results

```csharp
[Fact]
public async Task Handle_WhenResourceNotFound_ReturnsFailure()
{
    var handler = new GetByIdHandler(mockRepo);
    var result = await handler.Handle(new GetByIdQuery(999), CancellationToken.None);
    
    result.Should().BeOfType<Result<Dto>.Failure>();
    ((Result<Dto>.Failure)result).Code.Should().Be("NOT_FOUND");
}

[Fact]
public async Task Handle_WhenSuccessful_ReturnsSuccess()
{
    var handler = new GetByIdHandler(mockRepo);
    var result = await handler.Handle(new GetByIdQuery(1), CancellationToken.None);
    
    result.Should().BeOfType<Result<Dto>.Success>();
    ((Result<Dto>.Success)result).Data.Id.Should().Be(1);
}
```

### Integration Testing

```csharp
[Fact]
public async Task Post_WhenValidRequest_Returns201()
{
    var response = await _client.PostAsJsonAsync("/api/items", new CreateCommand(...));
    response.StatusCode.Should().Be(201);
}

[Fact]
public async Task Post_WhenConflict_Returns409()
{
    var response = await _client.PostAsJsonAsync("/api/items", new CreateCommand(...));
    response.StatusCode.Should().Be(409);
}
```

## Performance Considerations

- Result<T> is a record (value semantics)
- No boxing/unboxing needed
- No stack trace generation for expected errors
- Faster than exception throwing for error cases
- Minimal memory overhead

## Security Considerations

- Error codes should not leak sensitive information
- Details field should not include internal stack traces
- Controllers should validate error codes before mapping
- Never expose unvalidated user input in error messages
