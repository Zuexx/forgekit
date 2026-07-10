# ADR-006: Error Response Standardization with Machine-Readable Codes

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-003

## Context

Error handling in REST APIs creates design challenges:

1. **Client Error Handling:** How do clients know what went wrong? (network timeout vs validation vs permission denied?)
2. **User Communication:** What message should be shown to users? (technical details vs user-friendly message?)
3. **Logging/Monitoring:** How are errors tracked and analyzed? (error categorization, alerting)
4. **Field-Level Errors:** For validation, which fields failed and why?
5. **Tracing Issues:** How do we correlate errors between client logs and server logs?
6. **API Consistency:** Do all endpoints return errors in the same format?

ForgeKit requirements:
- Consistent error response format across entire API
- Machine-readable error codes for programmatic handling
- Human-readable messages for users
- Field-level validation error details
- Request correlation via TraceId
- RFC 7807 "Problem Details" compliance
- HTTP status codes that correctly reflect errors

## Decision

Implement **Standardized Error Response** with:

1. **ErrorResponse DTO:** Standardized response structure with Code, Message, Details
2. **ErrorCodes Constants:** Machine-readable codes (VALIDATION_ERROR, RESOURCE_NOT_FOUND, etc.)
3. **ExceptionHandlingMiddleware:** Centralized exception catching and formatting
4. **Field-Level Details:** Include field-specific errors for validation failures
5. **TraceId for Correlation:** Every error includes unique ID to correlate logs
6. **HTTP Status Code Mapping:** Each error code maps to correct HTTP status

### ErrorResponse DTO

```csharp
public sealed class ErrorResponse
{
    public string Message { get; set; }           // User-friendly message
    public string? Code { get; set; }             // Machine-readable code
    public DateTime Timestamp { get; set; }       // When error occurred
    public string? TraceId { get; set; }          // Request correlation ID
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }  // Field errors
    public string? Title { get; set; }            // RFC 7807 title
    public int? Status { get; set; }              // HTTP status code
    public string? Detail { get; set; }           // RFC 7807 detail
}
```

### Error Codes Constants

```csharp
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ConflictError = "CONFLICT_ERROR";
    public const string UnauthorizedError = "UNAUTHORIZED_ERROR";
    public const string BusinessLogicError = "BUSINESS_LOGIC_ERROR";
    public const string InvalidStateError = "INVALID_STATE_ERROR";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
```

## Rationale

### Why Standardized Error Responses?

1. **Predictable Client Handling:**
   - Clients know exact structure of error responses
   - Can parse and display errors consistently
   - Different error types handled differently (retry logic, user messaging)

2. **Machine-Readable Codes:**
   - Clients programmatically identify error type without parsing message
   - Message text can change without breaking client logic
   - Enables "switch" statements in client code:
     ```typescript
     switch(error.code) {
       case "VALIDATION_ERROR": showFormErrors(error.errors); break;
       case "RESOURCE_NOT_FOUND": showNotFoundMessage(); break;
       case "UNAUTHORIZED_ERROR": redirectToLogin(); break;
     }
     ```

3. **Field-Level Validation Details:**
   - Users immediately see which fields are invalid and why
   - Better UX than generic "validation failed" message
   - Frontend can highlight specific form fields
   - Example:
     ```json
     {
       "code": "VALIDATION_ERROR",
       "errors": {
         "dueDate": ["Must be in the future"],
         "workspaceId": ["Workspace not found"]
       }
     }
     ```

4. **Request Correlation/Tracing:**
   - Every error includes TraceId
   - Users can provide TraceId when reporting issues
   - Server logs can find exact request that failed
   - Distributed tracing across microservices
   - Example: "Reference this error in support: 0HN1GG2P6JFBM:00000001"

5. **RFC 7807 Compliance:**
   - Industry-standard format (adopted by many frameworks)
   - Tools and libraries built around Problem Details
   - Easy integration with error aggregation services
   - Future-proof (standard won't change)

6. **Consistent HTTP Status Codes:**
   - 422: Validation
   - 400: Business logic, bad request
   - 401/403: Authentication/authorization
   - 404: Resource not found
   - 409: Conflict
   - 500: Internal server error
   - Clients rely on status codes for retry logic

7. **Centralized Exception Handling:**
   - Middleware catches all exceptions
   - Consistent error responses everywhere
   - Easy to add cross-cutting concerns (logging, monitoring, alerting)
   - No individual endpoint error handling needed

8. **Production Safety:**
   - Internal exception details never leaked to clients
   - Sensitive information scrubbed
   - Stack traces only in logs, not in responses
   - Generic "Internal Server Error" for unhandled exceptions

## Alternatives Considered

### 1. No Standard Format (Each Endpoint Returns Different Structure)
**Approach:** Let each endpoint return errors in its own format

```csharp
// Endpoint 1
public class CreateVisitResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}

// Endpoint 2
public class UpdateVisitResponse
{
    public int ErrorCode { get; set; }
    public List<string> Errors { get; set; }
}

// Endpoint 3
public IResult DeleteVisit(string id)
{
    if (!found)
        return Results.NotFound("Resource not found");
    return Results.Ok();
}
```

**Pros:**
- Freedom to design errors per endpoint
- No shared infrastructure needed
- Simple for small APIs

**Cons:**
- Clients must handle multiple error formats
- Documentation burden (document each format)
- Inconsistent user experience
- Hard to write generic error handling client code
- Difficult to scale
- Nightmare for API consumers
- Makes client development harder

**When Better:** Throwaway APIs, internal tools with single client

---

### 2. Only HTTP Status Codes (No Message Body)
**Approach:** Use only HTTP status codes, rely on codes for meaning

```csharp
// Validation error
return StatusCode(400); // Client guesses what went wrong

// Resource not found
return NotFound(); // Client knows it's 404

// Conflict
return Conflict(); // Client knows it's 409
```

**Pros:**
- Minimalist approach
- No body overhead
- Clear HTTP semantics

**Cons:**
- No explanation of what went wrong
- Validation errors don't show which fields failed
- Impossible to distinguish types of 400 errors
- Poor user experience (no message)
- No TraceId for correlation
- Hard to debug

**When Better:** Simple APIs with trivial errors; internal services

---

### 3. Plain Text Error Messages
**Approach:** Return error as plain text string

```csharp
return BadRequest("Email is required and must be valid");
```

**Pros:**
- Simple
- Human-readable
- Minimal overhead

**Cons:**
- Not structured (can't parse programmatically)
- Hard to display multiple field errors
- Message text change breaks clients relying on message parsing
- No machine-readable codes
- Not standard
- No correlation IDs
- No trace information

**When Better:** Very simple APIs; forgekits

---

### 4. Custom Error Format (Not RFC 7807)
**Approach:** Create proprietary error format

```csharp
public class ApiError
{
    public string Type { get; set; }           // "validation", "not_found"
    public string Reason { get; set; }         // User message
    public Dictionary<string, string> Details { get; set; } // Field errors
}
```

**Pros:**
- Fully customizable
- Tailored to specific needs
- Can optimize for your use case

**Cons:**
- Not a standard (proprietary)
- Unfamiliar to developers (need documentation)
- Can't use standard error handling libraries
- Won't integrate with industry tools
- Makes client development harder
- If merged with other APIs, error format conflicts

**Trade-off:** Customization vs standardization

**When Better:** Very specific requirements; closed ecosystem

---

### 5. Exception Type in Response (Not Error Codes)
**Approach:** Return exception type name instead of machine-readable code

```csharp
public class ErrorResponse
{
    public string ExceptionType { get; set; }  // "ValidationAppException"
    public string Message { get; set; }
}

// Client code
if (error.ExceptionType == "ValidationAppException")
    handleValidation();
else if (error.ExceptionType == "NotFoundException")
    handleNotFound();
```

**Pros:**
- Maps directly to server exception types
- Easy to implement

**Cons:**
- Couples client to server exception names
- Exception names can change
- Not user-friendly
- Leaks internal implementation
- Hard to localize ("ValidationAppException" in error response)
- Not standardized
- Not RFC 7807 compliant

**When Better:** Very simple internal services

---

### 6. Separate Error Code Endpoint
**Approach:** Return error ID, clients fetch meaning from separate endpoint

```csharp
// Error response
{ "errorId": "VE-12345" }

// Client must call
GET /api/errors/VE-12345
// Response: { "code": "VALIDATION_ERROR", "message": "..." }
```

**Pros:**
- Minimal error response size
- Centralized error meaning

**Cons:**
- Extra network call needed
- Slow (must fetch error details separately)
- Impractical for validation errors
- Defeats name of having error details
- Unnecessary complexity

**When Better:** Never really (over-engineered)

---

## Consequences

### Positive

1. **Predictable Error Handling:** Clients know exactly what error looks like
2. **Programmatic Error Handling:** Can switch on error codes
3. **Better UX:** Users see which fields have errors
4. **Debugging:** TraceId enables finding exact request in logs
5. **Monitoring:** Error codes enable categorization and alerting
6. **Standards Compliance:** RFC 7807 compatible
7. **Framework Support:** Libraries built around Problem Details
8. **Consistency:** Every error formatted the same way
9. **Production Safety:** Sensitive details never leaked
10. **Scalability:** Works same whether 1 endpoint or 1000

### Negative

1. **Response Size:** Errors include more data than minimal response
2. **Implementation Overhead:** Requires middleware and exception types
3. **Error Code Synchronization:** Keep codes in sync across services/clients
4. **Exception Mapping:** Must map each exception type to error code/status
5. **Localization Complexity:** Messages need localization/translation

### Neutral

1. **HTTP Status Codes:** Must still use standard status codes (not changed)
2. **Backward Compatibility:** If existing API has different format, migration needed

## When to Use

✅ **Use Standardized Error Responses when:**
- Building REST API (especially public APIs)
- Want consistent error handling across endpoints
- Clients need to programmatically handle errors
- Need validation error details
- Require audit/tracing capabilities
- Building modern .NET API
- Want RFC 7807 compliance
- Multiple teams consuming API
- Error correlation/debugging important

✅ **Specifically for:**
- All REST APIs (standard practice)
- Public/external APIs
- Internal APIs with multiple clients
- Microservices (interoperability)

## When NOT to Use

❌ **Avoid when:**
- Building gRPC API (has different standard)
- GraphQL API (has different error model)
- Simple scripts or CLIs
- Throwaway forgekits
- Internal utilities with single known client
- Minimal response overhead critical

❌ **Don't use for:**
- Non-HTTP protocols
- Streaming responses
- Raw data APIs

## ForgeKit Implementation

### Error Response DTO

```csharp
// ForgeKit.Api/Models/ErrorResponse.cs
namespace Api.Models;

/// <summary>
/// Standardized error response for all API error scenarios.
/// Based on RFC 7807 Problem Details for HTTP APIs.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// User-friendly error message describing what went wrong.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code for programmatic error handling.
    /// Examples: VALIDATION_ERROR, RESOURCE_NOT_FOUND, UNAUTHORIZED_ERROR
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// UTC timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Unique identifier for tracing and correlating logs.
    /// Use to correlate error responses with server logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Field-level validation or business rule errors.
    /// Maps field names to arrays of error messages.
    /// Only populated for validation errors.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// RFC 7807 title (short summary of problem type).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// RFC 7807 detail (explanation specific to this occurrence).
    /// </summary>
    public string? Detail { get; set; }
}
```

### Error Codes Constants

```csharp
// ForgeKit.Api/Constants/ErrorCodes.cs
namespace Api.Constants;

/// <summary>
/// Standardized error codes for API responses.
/// Machine-readable codes for programmatic client error handling.
/// Each code corresponds to specific exception type and HTTP status.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Validation error (HTTP 422).
    /// Thrown when input validation fails (required fields, invalid format).
    /// </summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>
    /// Resource not found (HTTP 404).
    /// Thrown when requested resource doesn't exist.
    /// </summary>
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    /// <summary>
    /// Conflict error (HTTP 409).
    /// Thrown when resource conflict occurs (duplicate, state mismatch).
    /// </summary>
    public const string ConflictError = "CONFLICT_ERROR";

    /// <summary>
    /// Unauthorized/Forbidden (HTTP 403).
    /// Thrown when user lacks permission for operation.
    /// </summary>
    public const string UnauthorizedError = "UNAUTHORIZED_ERROR";

    /// <summary>
    /// Business logic error (HTTP 400).
    /// Thrown when business rule is violated.
    /// Example: Restoring entity beyond grace period.
    /// </summary>
    public const string BusinessLogicError = "BUSINESS_LOGIC_ERROR";

    /// <summary>
    /// Invalid state error (HTTP 400).
    /// Thrown when entity is in invalid state for operation.
    /// </summary>
    public const string InvalidStateError = "INVALID_STATE_ERROR";

    /// <summary>
    /// Internal server error (HTTP 500).
    /// Thrown when unhandled exception occurs on server.
    /// </summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
```

### Exception Handling Middleware

```csharp
// ForgeKit.Api/Middlewares/ExceptionHandlingMiddleware.cs
using Api.Constants;
using Api.Exceptions;
using Api.Models;
using System.Text.Json;

namespace Api.Middlewares;

/// <summary>
/// Middleware for handling exceptions and returning standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception occurred: {ExceptionMessage}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = MapExceptionToErrorResponse(exception, context);

            response.StatusCode = errorResponse.Status ?? StatusCodes.Status500InternalServerError;

            var json = JsonSerializer.Serialize(errorResponse,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await response.WriteAsync(json);
        }
    }

    private ErrorResponse MapExceptionToErrorResponse(Exception exception, HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        var timestamp = DateTime.UtcNow;

        return exception switch
        {
            ValidationAppException validationEx => new ErrorResponse
            {
                Message = "Validation failed",
                Code = ErrorCodes.ValidationError,
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred",
                Errors = validationEx.Errors,
                TraceId = traceId,
                Timestamp = timestamp
            },

            NotFoundException notFoundEx => new ErrorResponse
            {
                Message = notFoundEx.Message,
                Code = ErrorCodes.ResourceNotFound,
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFoundEx.Message,
                TraceId = traceId,
                Timestamp = timestamp
            },

            ConflictException conflictEx => new ErrorResponse
            {
                Message = conflictEx.Message,
                Code = ErrorCodes.ConflictError,
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflictEx.Message,
                TraceId = traceId,
                Timestamp = timestamp
            },

            UnauthorizedException unauthorizedEx => new ErrorResponse
            {
                Message = unauthorizedEx.Message,
                Code = ErrorCodes.UnauthorizedError,
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = unauthorizedEx.Message,
                TraceId = traceId,
                Timestamp = timestamp
            },

            BusinessLogicException businessEx => new ErrorResponse
            {
                Message = businessEx.Message,
                Code = ErrorCodes.BusinessLogicError,
                Status = StatusCodes.Status400BadRequest,
                Title = "Business Logic Error",
                Detail = businessEx.Message,
                TraceId = traceId,
                Timestamp = timestamp
            },

            _ => new ErrorResponse
            {
                Message = "An internal server error occurred",
                Code = ErrorCodes.InternalServerError,
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Please contact support with the TraceId",
                TraceId = traceId,
                Timestamp = timestamp
            }
        };
    }
}
```

### Middleware Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// ... other configuration ...

var app = builder.Build();

// Add exception handling middleware (should be early in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ... rest of app setup ...
```

### Exception Types

```csharp
// ForgeKit.Api/Exceptions/ValidationAppException.cs
public class ValidationAppException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}

// ForgeKit.Api/Exceptions/NotFoundException.cs
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// ForgeKit.Api/Exceptions/BusinessLogicException.cs
public class BusinessLogicException : Exception
{
    public BusinessLogicException(string message) : base(message) { }
}

// ForgeKit.Api/Exceptions/ConflictException.cs
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

// ForgeKit.Api/Exceptions/UnauthorizedException.cs
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
```

### Test Examples

```csharp
// ForgeKit.Api.Tests/Middlewares/ExceptionHandlingMiddlewareTests.cs
[TestFixture]
public class ExceptionHandlingMiddlewareTests
{
    [Test]
    public async Task ValidationException_ReturnsValidationErrorResponse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/resources";

        var middleware = new ExceptionHandlingMiddleware(
            context => throw new ValidationAppException(new Dictionary<string, string[]>
            {
                { "workspaceId", new[] { "Required", "Must be valid" } },
                { "dueDate", new[] { "Must be in future" } }
            }),
            new NullLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.That(httpContext.Response.StatusCode, Is.EqualTo(422));

        httpContext.Response.Body.Position = 0;
        var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody);

        Assert.That(error.Code, Is.EqualTo(ErrorCodes.ValidationError));
        Assert.That(error.Errors, Contains.Key("workspaceId"));
        Assert.That(error.TraceId, Is.Not.Null);
    }

    [Test]
    public async Task NotFound_Returns404WithMessage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            context => throw new NotFoundException("Resource not found"),
            new NullLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.That(httpContext.Response.StatusCode, Is.EqualTo(404));

        httpContext.Response.Body.Position = 0;
        var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody);

        Assert.That(error.Code, Is.EqualTo(ErrorCodes.ResourceNotFound));
        Assert.That(error.Message, Is.EqualTo("Resource not found"));
    }

    [Test]
    public async Task UnhandledException_Returns500InternalServerError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            context => throw new InvalidOperationException("Unexpected error"),
            new NullLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.That(httpContext.Response.StatusCode, Is.EqualTo(500));

        httpContext.Response.Body.Position = 0;
        var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody);

        Assert.That(error.Code, Is.EqualTo(ErrorCodes.InternalServerError));
        Assert.That(error.Message, Is.EqualTo("An internal server error occurred"));
        Assert.That(error.TraceId, Is.Not.Null);
    }
}
```

### Integration Test

```csharp
// ForgeKit.Api.Tests/Integration/Middlewares/ExceptionHandlingIntegrationTests.cs
[TestFixture]
public class ExceptionHandlingIntegrationTests
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
    public async Task CreateResource_WithValidationFailure_Returns422WithFieldErrors()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/v1/resources", new
        {
            workspaceId = "",  // Invalid
            name = "",  // Invalid
            dueDate = DateTime.UtcNow.AddDays(-1)  // Invalid
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status422UnprocessableEntity));

        var content = await response.Content.ReadAsAsync<ErrorResponse>();
        Assert.That(content.Code, Is.EqualTo(ErrorCodes.ValidationError));
        Assert.That(content.Errors, Is.Not.Null);
        Assert.That(content.Errors, Contains.Key("workspaceId"));
        Assert.That(content.Errors, Contains.Key("dueDate"));
        Assert.That(content.TraceId, Is.Not.Null);
    }

    [Test]
    public async Task GetResource_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/v1/resources/non-existent-id");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));

        var content = await response.Content.ReadAsAsync<ErrorResponse>();
        Assert.That(content.Code, Is.EqualTo(ErrorCodes.ResourceNotFound));
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

## Client Example (TypeScript)

```typescript
// client/services/api.ts
interface ApiErrorResponse {
    code: string;
    message: string;
    errors?: { [field: string]: string[] };
    traceId?: string;
}

async function handleApiError(response: Response): Promise<never> {
    const error: ApiErrorResponse = await response.json();

    switch (error.code) {
        case "VALIDATION_ERROR":
            // Show field-specific errors
            displayFormErrors(error.errors);
            throw new ValidationError(error.errors);

        case "RESOURCE_NOT_FOUND":
            showNotification("The requested item was not found", "error");
            throw new NotFoundError(error.message);

        case "UNAUTHORIZED_ERROR":
            // Redirect to login
            redirectToLogin();
            throw new UnauthorizedError(error.message);

        case "CONFLICT_ERROR":
            showNotification("A conflict occurred, please refresh and try again", "error");
            throw new ConflictError(error.message);

        default:
            // Generic error with trace ID for support
            showNotification(
                `An error occurred (Reference: ${error.traceId}). Please contact support.`,
                "error"
            );
            throw new ApiError(error.message, error.traceId);
    }
}
```

## Related ADRs

- **ADR-003:** Validation in pipeline (produces VALIDATION_ERROR responses)
- **ADR-001:** Module pattern (error responses consistent across modules)

## References

- [RFC 7807: Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core Exception Handling Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [API Error Handling Best Practices](https://www.rfc-editor.org/rfc/rfc7231#section-6)
