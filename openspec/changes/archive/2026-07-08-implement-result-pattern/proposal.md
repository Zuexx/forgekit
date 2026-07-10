# Proposal: Implement Result Pattern for Handler Responses

**Date:** 2026-02-04  
**Status:** PROPOSED  
**Priority:** HIGH  
**Scope:** API Handlers, MediatR Handlers  
**Complexity:** MEDIUM

---

## Executive Summary

Implement a **Result Pattern** (sometimes called Railway-Oriented Programming) to provide a cleaner, more explicit way to handle success and failure responses in MediatR handlers. This complements the exception-based error handling by providing an alternative for expected business errors. Result types carry both data and error information explicitly, enabling type-safe error handling throughout the application.

**Key Benefits:**
- ✅ Explicit success/failure handling without try-catch
- ✅ Type-safe error information for all operation outcomes
- ✅ Functional programming approach with composable operations
- ✅ Better clarity in handler contracts about possible outcomes
- ✅ Reduced exception overhead for expected errors (no stack traces)
- ✅ Complements exception-based error handling strategy

---

## Current State

### How We Currently Handle Errors

```csharp
// Current approach: Throw exceptions for expected errors
public class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, ResourceDto>
{
    public async Task<ResourceDto> Handle(GetResourceByIdQuery request, CancellationToken ct)
    {
        var resource = await _repository.GetAsync(request.ResourceId, ct);
        
        if (resource == null)
            throw new NotFoundException($"Resource {request.ResourceId} not found", "Resource");
            
        return _mapper.Map<ResourceDto>(resource);
    }
}
```

**Current Characteristics:**
- Uses exception-based control flow for expected errors
- Handler contract doesn't indicate what errors might occur
- Stack traces generated for expected business errors
- Difficult to distinguish expected vs unexpected errors at handler level

**Problems with Current Approach:**
- Exceptions are used for control flow (expected errors)
- Handler signature doesn't communicate what errors might occur
- No compile-time guarantee all cases are handled
- Stack traces overhead for expected business errors
- Mixed with infrastructure exceptions, harder to handle appropriately

---

## Proposed Solution

### Result Pattern Structure

Define a `Result<T>` type that represents either success or failure using discriminated unions:

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    public sealed record Failure(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null) : Result<T>;
}
```

This pattern works universally across all handlers and domains.

### How Handlers Would Look

```csharp
// New approach: Return Result<T> for explicit error handling
public class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, Result<ResourceDto>>
{
    public async Task<Result<ResourceDto>> Handle(GetResourceByIdQuery request, CancellationToken ct)
    {
        var resource = await _repository.GetAsync(request.ResourceId, ct);
        
        if (resource == null)
            return new Result<ResourceDto>.Failure(
                Code: "RESOURCE_NOT_FOUND",
                Message: $"Resource with ID {request.ResourceId} not found"
            );
            
        return new Result<ResourceDto>.Success(_mapper.Map<ResourceDto>(resource));
    }
}
```

### Controller Integration

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ResourceDto>> GetResource(string id)
{
    var result = await _mediator.Send(new GetResourceByIdQuery(id));
    
    return result switch
    {
        Result<ResourceDto>.Success success => Ok(success.Data),
        Result<ResourceDto>.Failure failure => Problem(
            detail: failure.Message,
            title: failure.Code,
            statusCode: MapErrorCodeToHttpStatus(failure.Code)
        )
    };
}
```

## Benefits

### 1. **Explicit Error Handling**
- Handler clearly shows what success/failure looks like
- No hidden exceptions in handler logic
- Controller code explicitly handles all cases (compiler ensures this)

### 2. **Type Safety**
- Compiler ensures all cases are handled
- IDE auto-complete for result properties
- No runtime surprises - errors are part of the type contract

### 3. **Performance**
- No exception stack trace overhead for expected errors
- Exceptions reserved for truly exceptional (unexpected) cases
- Reduced memory allocation and GC pressure

### 4. **Testability**
- Easy to test both success and failure paths
- No exception mocking or re-throwing needed
- Clear, readable assertion patterns

### 5. **Functional Composition**
- Chain operations with `.Map()`, `.Bind()`, etc.
- Supports monadic operations for composing complex logic
- More elegant than nested try-catch blocks

### 6. **Coexistence with Exceptions**
- Result pattern doesn't replace exception handling
- Both patterns can coexist in the same application
- Complements the exception-based error handling already in place
- No breaking changes to existing handlers using exceptions

---

## Implementation Plan

### Phase 1: Create Result Types
- Define `Result<T>` base type
- Create `Success` and `Failure` sealed records
- Add extension methods (Map, Bind, Match)
- Unit tests for Result type

### Phase 2: Create Conversion Extensions
- Create extensions to convert exceptions to Results
- Create extensions for common patterns
- Integration with existing exception types

### Phase 3: Update Sample Handlers
- Update 3-5 handlers as examples
- Document patterns and conventions
- Show best practices

### Phase 4: Create Guidelines
- Document when to use Result vs Exception
- Provide handler implementation guide
- Document controller integration patterns

### Phase 5: Integration with Middleware
- Update exception middleware to handle Result failures
- Ensure backward compatibility
- Verify error response format

---

## Coexistence with Exception Pattern

### What Changes
- **Business Errors:** Use Result pattern (NotFoundException → Result.Failure)
- **Unexpected Errors:** Keep exception throwing
- **Middleware:** Already catches exceptions and converts to error responses

### No Breaking Changes
- Existing exception-based handlers keep working
- New handlers adopt Result pattern gradually
- Can migrate incrementally

### Gradual Migration Path
```
Phase 1: New handlers use Result
Phase 2: Gradually migrate existing high-traffic handlers
Phase 3: Update remaining handlers
Phase 4: Make Result pattern standard
```

---

## Technical Approach

### Result Type Definition

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    
    public sealed record Failure(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null
    ) : Result<T>;
    
    // Extensions for functional operations
    public Result<TNext> Map<TNext>(Func<T, TNext> map) =>
        this switch
        {
            Success s => new Result<TNext>.Success(map(s.Data)),
            Failure f => new Result<TNext>.Failure(f.Code, f.Message, f.Details),
            _ => throw new InvalidOperationException()
        };
    
    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind) =>
        this switch
        {
            Success s => bind(s.Data),
            Failure f => new Result<TNext>.Failure(f.Code, f.Message, f.Details),
            _ => throw new InvalidOperationException()
        };
    
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, string, IReadOnlyDictionary<string, string[]>?, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Data),
            Failure f => onFailure(f.Code, f.Message, f.Details),
            _ => throw new InvalidOperationException()
        };
}
```

### Handler Example

```csharp
public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Result<TodoItemDto>>
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    
    public async Task<Result<TodoItemDto>> Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        // Validation: Visit date must be at least 5 days in advance
        if (request.VisitDate < DateTime.Today.AddDays(5))
            return new Result<TodoItemDto>.Failure(
                Code: "INVALID_VISIT_DATE",
                Message: "Visit requests must be made at least 5 days in advance"
            );
        
        // Verify MR exists
        var mr = await _context.Members
            .FirstOrDefaultAsync(m => m.Id == request.MrId, ct);
        if (mr == null)
            return new Result<TodoItemDto>.Failure(
                Code: "MR_NOT_FOUND",
                Message: "Medical Representative not found"
            );
        
        // Verify HCO exists
        var hco = await _context.Workspaces
            .FirstOrDefaultAsync(h => h.Id == request.HcoId, ct);
        if (hco == null)
            return new Result<TodoItemDto>.Failure(
                Code: "HCO_NOT_FOUND",
                Message: "Healthcare Organization not found"
            );
        
        // Check for scheduling conflicts (MR can't have multiple visits on same day)
        var conflictingVisit = await _context.TodoItems
            .FirstOrDefaultAsync(v => v.MrId == request.MrId 
                && v.VisitDate == request.VisitDate
                && v.CurrentStatus != "Cancelled", ct);
        if (conflictingVisit != null)
            return new Result<TodoItemDto>.Failure(
                Code: "VISIT_CONFLICT",
                Message: "Medical Representative already has a visit scheduled for this date"
            );
        
        // Create visit request
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid().ToString("N").ToLower(),
            MrId = request.MrId,
            HcoId = request.HcoId,
            HcpId = request.HcpId,
            VisitDate = request.VisitDate,
            Purpose = request.Purpose,
            CurrentStatus = "Requested",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };
        
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync(ct);
        
        return new Result<TodoItemDto>.Success(_mapper.Map<TodoItemDto>(todoItem));
    }
}
```

### Controller Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> CreateTodoItem(CreateTodoItemCommand command)
    {
        var result = await _mediator.Send(command);
        
        return result switch
        {
            Result<TodoItemDto>.Success success => 
                CreatedAtAction(nameof(GetTodoItem), new { id = success.Data.Id }, success.Data),
            
            Result<TodoItemDto>.Failure failure => 
                Problem(
                    detail: failure.Message,
                    title: failure.Code,
                    statusCode: MapErrorCodeToStatus(failure.Code),
                    extensions: failure.Details != null ? 
                        new Dictionary<string, object?> { { "errors", failure.Details } } : 
                        null
                ),
            
            _ => StatusCode(500)
        };
    }
    
    private int MapErrorCodeToStatus(string code) => code switch
    {
        "INVALID_VISIT_DATE" => StatusCodes.Status400BadRequest,
        "VISIT_CONFLICT" => StatusCodes.Status409Conflict,
        "MR_NOT_FOUND" => StatusCodes.Status404NotFound,
        "HCO_NOT_FOUND" => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    };
}
```

---

## Comparison: Exception vs Result Pattern

| Aspect | Exception Pattern | Result Pattern |
|--------|-------------------|-----------------|
| Success Path | Direct return | Success case |
| Error Path | Throw exception | Failure case |
| Control Flow | Exception handling | Pattern matching |
| Stack Trace | Always generated | Only for unexpected |
| Type Safety | No | Yes |
| Composition | Difficult | Easy (monadic) |
| Performance | Overhead for errors | No overhead |
| Testing | Exception mocking | Direct assertion |
| Intent Clarity | Hidden in throws | Explicit in type |

---

## Risks & Mitigations

### Risk 1: Inconsistent Adoption
**Mitigation:**
- Create clear guidelines
- Provide code templates
- Regular reviews during PRs

### Risk 2: Mixing Patterns
**Mitigation:**
- Document when to use each
- Establish clear conventions
- Provide linting rules

### Risk 3: Performance Impact
**Mitigation:**
- Result is lightweight (just records)
- No overhead vs exceptions
- Actually better performance for error cases

### Risk 4: Learning Curve
**Mitigation:**
- Provide comprehensive examples
- Create training documentation
- Start with sample handlers

---

## Success Criteria

- ✅ Result<T> type fully implemented and tested
- ✅ 5+ handlers migrated to use Result pattern
- ✅ Controller integration patterns documented
- ✅ Backward compatibility maintained
- ✅ No performance regression
- ✅ Tests passing for all implementations
- ✅ Team trained and conventions established

---

## Questions to Address

1. **Should we use Result<T> everywhere or only for queries?**
   - Recommendation: Use for queries and commands (all handlers)

2. **How do we handle nested Results (async composition)?**
   - Recommendation: Provide `.BindAsync()` extension method

3. **Should validation errors be in Result.Failure or throw?**
   - Recommendation: Use Result.Failure for explicit validation

4. **How does this integrate with FluentValidation?**
   - Recommendation: Add FluentValidation behavior that returns Result

5. **Should middleware convert Result.Failure to exceptions?**
   - Recommendation: No, middleware only handles exceptions; controllers handle Results

---

## Related Standards & Patterns

- Railway-Oriented Programming (Scott Wlaschin)
- Result/Either Monads in functional languages
- Option/Result types in Rust
- Discriminated Unions in TypeScript

---

## Next Steps

1. **Get approval** on this proposal
2. **Create design document** with detailed specifications
3. **Implement Result<T> type** with extensions
4. **Create sample handlers** demonstrating patterns
5. **Update guidelines** for handler implementation
6. **Begin gradual migration** of existing handlers

---

## References

- Railway-Oriented Programming: https://fsharpforfunandprofit.com/rop/
- Functional Programming in C#: Using Monads & Applicative Functors
- Discriminated Unions: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions
