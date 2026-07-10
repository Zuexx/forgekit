# Design: Validation Error Response Standardization

**Version:** 1.0  
**Last Updated:** 2026-02-09  

## Architecture Overview

```
HTTP Request
    ↓
Exception Occurs (Validation or Business Logic)
    ↓
ExceptionHandlingMiddleware catches exception
    ↓
Route to appropriate handler based on exception type
    ↓
Return ErrorResponse DTO with standardized format
    ↓
HTTP Response (JSON)
```

## Components

### 1. ErrorResponse DTO

**File:** `Api/Models/ErrorResponse.cs`

```csharp
namespace Api.Models;

/// <summary>
/// Standardized error response returned by API for all error scenarios.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// User-friendly error message.
    /// </summary>
    /// <example>Validation failed</example>
    public string Message { get; set; }

    /// <summary>
    /// Machine-readable error code for programmatic handling.
    /// </summary>
    /// <example>VALIDATION_ERROR</example>
    public string? Code { get; set; }

    /// <summary>
    /// UTC timestamp when error occurred.
    /// </summary>
    /// <example>2026-02-09T12:00:00Z</example>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Trace ID for correlating logs with requests.
    /// </summary>
    /// <example>0HN1GG2P6JFBM:00000001</example>
    public string? TraceId { get; set; }

    /// <summary>
    /// Field-level validation errors. Only populated for validation errors.
    /// Key: Field name, Value: Array of error messages
    /// </summary>
    /// <example>
    /// {
    ///   "dueDate": ["Must be a future date"],
    ///   "name": ["Cannot be empty"]
    /// }
    /// </example>
    public Dictionary<string, string[]>? Errors { get; set; }
}
```

### 2. Error Code Constants

**File:** `Api/Constants/ErrorCodes.cs`

```csharp
namespace Api.Constants;

/// <summary>
/// Standardized error codes for API responses.
/// </summary>
public static class ErrorCodes
{
    /// <summary>Validation error (422)</summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>Resource not found (404)</summary>
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    /// <summary>Conflict/Duplicate (409)</summary>
    public const string ConflictError = "CONFLICT_ERROR";

    /// <summary>Unauthorized/Forbidden (403)</summary>
    public const string UnauthorizedError = "UNAUTHORIZED_ERROR";

    /// <summary>Internal server error (500)</summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    /// <summary>Business logic violation (400)</summary>
    public const string BusinessLogicError = "BUSINESS_LOGIC_ERROR";

    /// <summary>Invalid state (400)</summary>
    public const string InvalidStateError = "INVALID_STATE_ERROR";
}
```

### 3. Enhanced ExceptionHandlingMiddleware

**File:** `Api/Middlewares/ExceptionHandlingMiddleware.cs`

```csharp
namespace Api.Middlewares;

using Api.Constants;
using Api.Exceptions;
using Api.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for handling all unhandled exceptions and returning standardized error responses.
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = exception.Message,
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ValidationAppException vex:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                response.Code = ErrorCodes.ValidationError;
                response.Errors = vex.Errors?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray());
                break;

            case NotFoundException nex:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Code = ErrorCodes.ResourceNotFound;
                break;

            case ConflictException cex:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response.Code = ErrorCodes.ConflictError;
                break;

            case UnauthorizedException uex:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                response.Code = ErrorCodes.UnauthorizedError;
                break;

            case BusinessLogicException bex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Code = ErrorCodes.BusinessLogicError;
                break;

            case InvalidStateException iex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Code = ErrorCodes.InvalidStateError;
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Code = ErrorCodes.InternalServerError;
                response.Message = "An internal server error occurred.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

## Example Error Responses

### Validation Error (422)
```json
{
  "message": "One or more validation errors occurred.",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-02-09T12:00:00Z",
  "traceId": "0HN1GG2P6JFBM:00000001",
  "errors": {
    "dueDate": ["Must be a future date"],
    "name": ["Cannot be empty"],
    "duration": ["Must be greater than 0"]
  }
}
```

### Resource Not Found (404)
```json
{
  "message": "Resource not found.",
  "code": "RESOURCE_NOT_FOUND",
  "timestamp": "2026-02-09T12:01:00Z",
  "traceId": "0HN1GG2P6JFBM:00000002"
}
```

### Business Logic Error (400)
```json
{
  "message": "Cannot restore resource. Soft-deleted entities can only be restored within 30 days.",
  "code": "BUSINESS_LOGIC_ERROR",
  "timestamp": "2026-02-09T12:02:00Z",
  "traceId": "0HN1GG2P6JFBM:00000003"
}
```

### Conflict Error (409)
```json
{
  "message": "Resource already exists for this workspace.",
  "code": "CONFLICT_ERROR",
  "timestamp": "2026-02-09T12:03:00Z",
  "traceId": "0HN1GG2P6JFBM:00000004"
}
```

### Internal Server Error (500)
```json
{
  "message": "An internal server error occurred.",
  "code": "INTERNAL_SERVER_ERROR",
  "timestamp": "2026-02-09T12:04:00Z",
  "traceId": "0HN1GG2P6JFBM:00000005"
}
```

## Integration Points

### 1. Program.cs Configuration
No additional DI configuration needed - middleware uses existing exception types.

### 2. ValidationBehavior Pipeline
Already working correctly; no changes needed. ErrorResponse structure accommodates field-level errors.

### 3. OpenAPI Documentation
Swagger/Scalar should document ErrorResponse schema so clients understand format.

## Benefits

- ✅ **Consistent API Responses:** All errors follow same format
- ✅ **Programmatic Error Handling:** Error codes enable client-side logic
- ✅ **Error Tracking:** TraceId correlates errors with logs
- ✅ **Field-Level Validation:** Clients know exactly which fields failed
- ✅ **Backward Compatible:** No breaking changes to existing code
- ✅ **Future Proof:** Foundation for error analytics/monitoring

## Testing Strategy

- Unit tests for middleware error mapping
- Integration tests for full request-response cycle
- Validation behavior tests to ensure error details propagate
- No new tests required; existing exception handling tests sufficient

## Migration Notes

- Current code using `ValidationAppException` works without changes
- Error response structure automatically populated by middleware
- Clients can immediately use error codes for better handling
