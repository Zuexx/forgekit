# ADR-003: Validation in Pipeline with MediatR

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-001, ADR-006

## Context

Validation is a cross-cutting concern in command/query handlers. Without a structured approach:
- Validation logic scatters across multiple handlers (copy-paste, inconsistency)
- Early returns for validation fill handler code (violates single responsibility)
- Validation logic is difficult to test separately from handler logic
- Validation order and reporting becomes inconsistent
- Custom validators for domain models can't be reused
- Testing validation requires testing the entire handler

ForgeKit requires:
- Unified validation across all commands and queries
- Complex validation rules (visiting future dates, checking business entity constraints)
- Clear validation error reporting with field-level details
- Separation of concerns (validation vs business logic)
- Reusable validators that work across multiple handlers

## Decision

Implement **MediatR Pipeline-Based Validation** where:

1. **Validation Behavior:** Create an `IPipelineBehavior<TRequest, TResponse>` that intercepts all requests
2. **FluentValidation Integration:** Use FluentValidation for declarative validation rules
3. **Pre-Handler Validation:** Validate all requests before handlers execute (fail fast)
4. **Automatic Discovery:** Validators are auto-discovered via reflection and injected
5. **Detailed Error Response:** Return field-level validation errors in structured format
6. **Single Exception Type:** All validation failures throw `ValidationAppException` with errors dictionary

### Validation Behavior

```csharp
namespace Api.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var errorsDictionary = validators
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x != null)
            .GroupBy(x => x.PropertyName, x => x.ErrorMessage,
                (propertyName, errorMessages) => new
                {
                    Key = propertyName,
                    Values = errorMessages.Distinct().ToArray()
                })
            .ToDictionary(x => x.Key, x => x.Values);

        if (errorsDictionary.Count > 0)
            throw new ValidationAppException(errorsDictionary);

        return await next(cancellationToken);
    }
}
```

## Rationale

### Why Pipeline-Based Validation?

1. **Separation of Concerns:**
   - Handlers focus on business logic, not validation
   - Validation logic is isolated and testable
   - Handlers become cleaner and more focused

2. **DRY Principle:**
   - Validators written once, used across all handlers using that request
   - Multiple handlers for same request type (if they exist) share validation
   - No code duplication across similar handlers

3. **Declarative vs Imperative:**
   - FluentValidation allows declarative rules:
     ```csharp
     RuleFor(x => x.VisitDate).Must(date => date > DateTime.UtcNow);
     ```
   - More readable than imperative `if (x.VisitDate <= DateTime.UtcNow) throw ...`
   - Business analysts can understand rules without code knowledge

4. **Automatic Discovery:**
   - Validators discovered via reflection and injected automatically
   - No manual registration or ordering needed
   - New validators automatically picked up by all requests

5. **Cross-Cutting Consistency:**
   - Same validation approach used everywhere
   - Consistent error responses across entire API
   - Single point to change validation behavior

6. **Testability:**
   - Validators tested independently of handlers
   - Handlers tested without validation concerns
   - Easier to test "happy path" without validation boilerplate

7. **Async Validation Support:**
   - Can validate against database (e.g., "check if user exists")
   - Can call external services (e.g., verify with payment provider)
   - Pipeline-based approach naturally supports async validators

8. **Performance:**
   - Fails fast before handler execution (no wasted computation)
   - Fewer lines of code to execute in handler happy path
   - Pipeline behavior can be optimized once, benefits all handlers

## Alternatives Considered

### 1. Validation in Handler (No Dedicated Validator)
**Approach:** Each handler contains its own validation logic

```csharp
public class CreateTodoItemHandler : IRequestHandler<CreateTodoItemCommand>
{
    public async Task Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        // Validation in handler
        if (string.IsNullOrEmpty(request.MrId))
            throw new ValidationAppException("MrId is required");
        if (string.IsNullOrEmpty(request.HcoId))
            throw new ValidationAppException("HcoId is required");
        if (request.VisitDate <= DateTime.UtcNow)
            throw new ValidationAppException("VisitDate must be in future");

        // Business logic
        var todoItem = new TodoItem { /* ... */ };
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

**Pros:**
- Simple, all logic in one place
- No framework dependency
- Explicit control over validation

**Cons:**
- Validation logic duplicated across similar handlers
- Handler becomes large and unfocused
- Validation not testable without executing handler
- Inconsistent validation approach if developers write differently
- Hard to reuse validators across handlers
- Difficult to maintain consistent error responses

**When Better:** Trivial applications with 1-2 handlers; simple validation needs

---

### 2. Repository/Validator Objects (Manual Registration)
**Approach:** Create separate validator classes, manually register each one

```csharp
public class CreateTodoItemValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemValidator(IVisitRepository repository)
    {
        RuleFor(x => x.MrId)
            .NotEmpty().WithMessage("MrId is required")
            .Must(mrId => repository.MemberExists(mrId))
            .WithMessage("Medical Representative not found");

        RuleFor(x => x.HcoId)
            .NotEmpty().WithMessage("HcoId is required")
            .Must(hcoId => repository.WorkspaceExists(hcoId))
            .WithMessage("Healthcare Organization not found");

        RuleFor(x => x.VisitDate)
            .Must(date => date > DateTime.UtcNow)
            .WithMessage("VisitDate must be in future");
    }
}

// In Program.cs
services.AddValidatorsFromAssemblyContaining<CreateTodoItemValidator>();
```

**Pros:**
- Reusable validators
- Clean separation of concerns
- Testable validators
- Explicit about dependencies

**Cons:**
- Requires manual registration (though AddValidatorsFromAssembly helps)
- Developer must remember to create validator for each request
- Easy to forget validator creation, leading to no validation
- Coupling to repositories (violates layer separation)
- More boilerplate than pipeline-based

**Trade-off:** Explicit registration vs automatic discovery

**When Better:** Very small team (< 3 people) where coordination is easy

---

### 3. Attribute-Based Validation (Data Annotations)
**Approach:** Use [Required], [Range], etc. attributes on request objects

```csharp
public class CreateTodoItemCommand : IRequest
{
    [Required(ErrorMessage = "MrId is required")]
    public string MrId { get; set; }

    [Required(ErrorMessage = "HcoId is required")]
    public string HcoId { get; set; }

    [DataType(DataType.Date)]
    [Range(typeof(DateTime), DateTime.UtcNow.ToString(), "12/31/2099")]
    public DateTime VisitDate { get; set; }
}

// In handler
public async Task Handle(CreateTodoItemCommand request, CancellationToken ct)
{
    // Validation happens automatically via ModelState in controller
    // No need for separate validator
}
```

**Pros:**
- Simple, built-in to ASP.NET
- Attributes visible on model definition
- No external framework for simple rules
- Automatic client-side validation

**Cons:**
- Limited to simple rules (can't call async validation)
- Can't reference dependencies (repositories, services)
- Error messages in attributes (harder to maintain/translate)
- Tight coupling of validation to model definition
- Doesn't work with complex business rules
- Model bloat with validation logic

**When Better:** REST APIs with simple field validation; UI forms with symmetrical validation

---

### 4. Validation Service/Facade Pattern
**Approach:** Inject IValidationService into handler, call it explicitly

```csharp
public interface IValidationService
{
    Task<ValidationResult> ValidateCreateTodoAsync(CreateTodoItemCommand request);
}

public class CreateTodoItemHandler : IRequestHandler<CreateTodoItemCommand>
{
    private readonly IValidationService _validationService;

    public async Task Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        var result = await _validationService.ValidateCreateTodoAsync(request);
        if (!result.IsValid)
            throw new ValidationAppException(result.Errors);

        // Business logic
    }
}
```

**Pros:**
- Explicit validation call (easy to see in handler)
- Validators reusable across handlers
- Can inject dependencies into validation service
- Supports async validation

**Cons:**
- Validation logic still scattered in handlers (each handler must call service)
- Easy to forget validation call (no compile-time guarantee)
- Boilerplate in every handler
- Easy to call validation at wrong point in handler
- Different handlers might validate differently

**Trade-off:** Explicit vs implicit; flexibility vs consistency

**When Better:** Handlers need custom validation logic per-handler; conditional validation needed

---

### 5. Custom Validation Middleware
**Approach:** Global middleware validates all incoming requests

```csharp
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IValidationService service)
    {
        // Validate request body
        if (context.Request.ContentType?.Contains("application/json") == true)
        {
            var request = await ReadRequestBodyAsync(context);
            var result = await service.ValidateAsync(request);

            if (!result.IsValid)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return; // Short-circuit before reaching handler
            }
        }

        await _next(context);
    }
}
```

**Pros:**
- Truly global validation
- Can validate before handler execution
- Single point for all validation logic
- No per-handler overhead

**Cons:**
- Works only for HTTP requests, not for internal command dispatching
- Generic validation without knowledge of specific request type
- Can't leverage FluentValidation's type-safe rules
- Difficult to provide context-aware error messages
- Requires reading request body (which can only be done once)
- Harder to compose with other middleware

**When Better:** Need to validate all incoming HTTP requests uniformly

---

## Consequences

### Positive

1. **Clean Handlers:** Business logic separate from validation concerns
2. **Reusable Validators:** Write once, validate everywhere that request is used
3. **Easy to Test:** Validators tested independently, handlers tested without validation
4. **Consistent Errors:** All validation failures return same structured error format
5. **Automatic Application:** New validators automatically picked up by all handlers
6. **DRY Code:** No validation logic duplication across handlers
7. **Maintainability:** Change validation rule in one place, affects all handlers
8. **Framework Standard:** MediatR pipelines are well-known pattern

### Negative

1. **Implicit Behavior:** Validation happens silently in pipeline (can be confusing for new developers)
2. **Hidden Performance:** Reflection and validator instantiation adds startup time (~10-50ms)
3. **Debugging Difficulty:** Stack trace shows ValidationBehavior frame (extra noise)
4. **Testing Overhead:** Must configure MediatR pipeline in tests, can't easily skip validation
5. **Generic Error Handling:** All validation errors treated the same way (no per-validator customization)

### Neutral

1. **Framework Dependency:** Requires MediatR and FluentValidation (but these are standard)
2. **Architectural Layer:** Adds abstraction layer between request and handler

## When to Use

✅ **Use Pipeline-Based Validation when:**
- Application has 3+ handlers (justifies validator reuse)
- Validation rules are complex or repeated
- Want separation of concerns (handler vs validation)
- Need consistent error responses across API
- Team wants to enforce consistent validation approach
- Want testable validation logic
- Need async validation (database/external service calls)
- Building .NET application with MediatR

✅ **Specifically for:**
- Command handlers (data modification requests)
- Query handlers with input validation
- Cross-handler shared validation rules

## When NOT to Use

❌ **Avoid Pipeline-Based Validation when:**
- Application has only 1-2 handlers
- Validation rules are trivial (required field, range check)
- Handler-specific validation logic needed
- Can't use MediatR (external API, legacy system)
- Team is unfamiliar with FluentValidation
- Every request type has unique validation

❌ **Don't use for:**
- Infrastructure/infrastructure concerns (not requests)
- Non-MediatR request types
- Validation that needs handler state/context

## ForgeKit Implementation

### FluentValidation Validators

```csharp
// ForgeKit.Api/Handlers/Visits/CreateTodoItemValidator.cs
using FluentValidation;
using Api.Handlers.Visits.Commands;

namespace Api.Handlers.Visits.Validators;

public class CreateTodoItemValidator : AbstractValidator<CreateTodoItemCommand>
{
    private readonly AppDbContext _dbContext;

    public CreateTodoItemValidator(AppDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.MrId)
            .NotEmpty().WithMessage("Medical Representative ID is required")
            .MustAsync(MemberExists)
            .WithMessage("Medical Representative not found");

        RuleFor(x => x.HcoId)
            .NotEmpty().WithMessage("Healthcare Organization ID is required")
            .MustAsync(WorkspaceExists)
            .WithMessage("Healthcare Organization not found");

        RuleFor(x => x.VisitDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Visit date must be in the future")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(365))
            .WithMessage("Visit date cannot be more than 1 year in the future");

        RuleFor(x => x.Purpose)
            .MaximumLength(500)
            .WithMessage("Purpose cannot exceed 500 characters");
    }

    private async Task<bool> MemberExists(
        string mrId,
        CancellationToken ct)
    {
        return await _dbContext.Set<Member>()
            .AnyAsync(m => m.Id == mrId, cancellationToken: ct);
    }

    private async Task<bool> WorkspaceExists(
        string hcoId,
        CancellationToken ct)
    {
        return await _dbContext.Set<Workspace>()
            .AnyAsync(h => h.Id == hcoId, cancellationToken: ct);
    }
}
```

### ValidationBehavior Pipeline

```csharp
// ForgeKit.Api/Behaviors/ValidationBehavior.cs
using Api.Exceptions;
using FluentValidation;
using MediatR;

namespace Api.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var errorsDictionary = _validators
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x != null)
            .GroupBy(
                x => x.PropertyName,
                x => x.ErrorMessage,
                (propertyName, errorMessages) => new
                {
                    Key = propertyName,
                    Values = errorMessages.Distinct().ToArray()
                })
            .ToDictionary(x => x.Key, x => x.Values);

        if (errorsDictionary.Count > 0)
            throw new ValidationAppException(errorsDictionary);

        return await next(cancellationToken);
    }
}
```

### MediatR Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Register MediatR with validation behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);

    // Register ValidationBehavior for all requests
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Register all validators from assembly
builder.Services.AddFluentValidation(cfg =>
{
    cfg.RegisterValidatorsFromAssemblies(typeof(Program).Assembly);
});

// ... rest of configuration
```

### Handler Implementation (No Validation)

```csharp
// ForgeKit.Api/Handlers/Visits/Commands/CreateTodoItemCommand.cs
public class CreateTodoItemCommand : IRequest<CreateTodoResult>
{
    public string MrId { get; set; }
    public string HcoId { get; set; }
    public DateTime VisitDate { get; set; }
    public string? Purpose { get; set; }
}

// ForgeKit.Api/Handlers/Visits/CreateTodoItemHandler.cs
public class CreateTodoItemHandler : IRequestHandler<CreateTodoItemCommand, CreateTodoResult>
{
    private readonly TodoService _visitService;

    public CreateTodoItemHandler(TodoService visitService)
    {
        _visitService = visitService;
    }

    public async Task<CreateTodoResult> Handle(
        CreateTodoItemCommand request,
        CancellationToken ct)
    {
        // Validation is guaranteed to have passed
        // Focus on business logic only
        var todoItem = await _todoService.CreateTodoAsync(
            request.MrId,
            request.HcoId,
            request.VisitDate,
            request.Purpose,
            ct);

        return new CreateTodoResult(todoItem.Id);
    }
}
```

### Test Examples

```csharp
// ForgeKit.Api.Tests/Handlers/Visits/CreateTodoItemValidatorTests.cs
[TestFixture]
public class CreateTodoItemValidatorTests
{
    private CreateTodoItemValidator _validator;
    private AppDbContext _dbContext;

    [SetUp]
    public async Task SetUp()
    {
        _dbContext = CreateTestDbContext();
        _validator = new CreateTodoItemValidator(_dbContext);

        // Seed test data
        var member = new Member { Id = "mr-123", Name = "John Doe" };
        var workspace = new Workspace { Id = "hco-123", Name = "Hospital A" };

        _dbContext.Add(mr);
        _dbContext.Add(hco);
        await _dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task Validate_WithValidRequest_ReturnsValid()
    {
        var request = new CreateTodoItemCommand
        {
            MrId = "mr-123",
            HcoId = "hco-123",
            VisitDate = DateTime.UtcNow.AddDays(7),
            Purpose = "Product training"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithPastDate_ReturnsFail()
    {
        var request = new CreateTodoItemCommand
        {
            MrId = "mr-123",
            HcoId = "hco-123",
            VisitDate = DateTime.UtcNow.AddDays(-1),
            Purpose = "Product training"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.GreaterThan(0));
        Assert.That(result.Errors[0].PropertyName, Is.EqualTo(nameof(CreateTodoItemCommand.VisitDate)));
    }

    [Test]
    public async Task Validate_WithNonExistentMr_ReturnsFail()
    {
        var request = new CreateTodoItemCommand
        {
            MrId = "non-existent-mr",
            HcoId = "hco-123",
            VisitDate = DateTime.UtcNow.AddDays(7),
            Purpose = "Product training"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }
}

// ForgeKit.Api.Tests/Handlers/Visits/CreateTodoItemHandlerTests.cs
[TestFixture]
public class CreateTodoItemHandlerTests
{
    private CreateTodoItemHandler _handler;
    private Mock<TodoService> _visitServiceMock;

    [SetUp]
    public void SetUp()
    {
        _visitServiceMock = new Mock<TodoService>();
        _handler = new CreateTodoItemHandler(_visitServiceMock.Object);
    }

    [Test]
    public async Task Handle_CallsVisitService()
    {
        var request = new CreateTodoItemCommand
        {
            MrId = "mr-123",
            HcoId = "hco-123",
            VisitDate = DateTime.UtcNow.AddDays(7),
            Purpose = "Training"
        };

        var todoItem = new TodoItem { Id = "visit-123" };
        _visitServiceMock
            .Setup(x => x.CreateTodoAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(todoItem);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result.VisitId, Is.EqualTo("visit-123"));
        _visitServiceMock.Verify(x => x.CreateTodoAsync(
            "mr-123", "hco-123", request.VisitDate, "Training", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Test

```csharp
// ForgeKit.Api.Tests/Integration/Handlers/CreateTodoItemIntegrationTests.cs
[TestFixture]
public class CreateTodoItemIntegrationTests
{
    private HttpClient _client;
    private TestWebApplicationFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task CreateVisit_WithInvalidData_Returns400WithValidationErrors()
    {
        var request = new CreateTodoItemRequest
        {
            MrId = "",  // Invalid: empty
            HcoId = "hco-123",
            VisitDate = DateTime.UtcNow.AddDays(-1)  // Invalid: past date
        };

        var response = await _client.PostAsJsonAsync("/v1/visits", request);

        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        var content = await response.Content.ReadAsAsync<ErrorResponse>();
        Assert.That(content.Code, Is.EqualTo(ErrorCodes.ValidationError));
        Assert.That(content.Errors, Contains.Key("MrId"));
        Assert.That(content.Errors, Contains.Key("VisitDate"));
    }
}
```

## Related ADRs

- **ADR-001:** Module pattern (modules contain handlers and validators)
- **ADR-006:** Error response standardization (validation errors use standardized format)

## References

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [MediatR Pipeline Behaviors](https://github.com/jbogard/MediatR/wiki)
- [CQRS Pattern with Validation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns)
