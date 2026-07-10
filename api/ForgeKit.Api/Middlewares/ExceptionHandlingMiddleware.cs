using System.Text.Json;
using ForgeKit.Api.Constants;
using ForgeKit.Api.Exceptions;
using ForgeKit.Api.Models;
using FluentValidation;

namespace ForgeKit.Api.Middlewares;

/// <summary>
/// Middleware for handling exceptions and converting them to standardized error responses.
/// 
/// This middleware:
/// - Catches all unhandled exceptions in the request pipeline
/// - Extracts or generates correlation IDs for request tracing
/// - Maps each exception type to an appropriate HTTP status code and error code
/// - Formats error responses with standardized ErrorResponse DTO
/// - Logs all errors with structured context including correlation ID
/// 
/// Error responses include:
/// - message: User-friendly error message
/// - code: Machine-readable error code for programmatic handling
/// - timestamp: UTC timestamp when error occurred
/// - traceId: Correlation ID for distributed tracing
/// - errors: Optional field-level error details (validation errors only)
/// - title, status, detail: RFC 7807 fields for backward compatibility
/// 
/// This centralized approach keeps exception handling consistent across all endpoints
/// and allows handlers to focus on business logic rather than error formatting.
/// </summary>
public sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = ExtractCorrelationId(context);
        context.Items[CorrelationIdHeaderName] = correlationId;

        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception with correlation ID {CorrelationId}: {Message}", 
                correlationId, e.Message);

            await HandleExceptionAsync(context, e, correlationId);
        }
    }

    /// <summary>
    /// Extracts or generates a correlation ID for request tracing.
    /// 
    /// The correlation ID is extracted from the X-Correlation-ID header if present.
    /// If not provided by the client, the request's built-in TraceIdentifier is used.
    /// The correlation ID is stored in HttpContext.Items for access by downstream handlers.
    /// </summary>
    private static string ExtractCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            var correlationId = correlationIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }
        }

        return context.TraceIdentifier;
    }

    private static async Task HandleExceptionAsync(HttpContext httpContext, Exception exception, string correlationId)
    {
        var statusCode = GetStatusCode(exception);
        var title = GetTitle(exception);
        var code = GetErrorCode(exception);
        var errors = GetErrors(exception);

        var response = new ErrorResponse
        {
            Message = exception.Message,
            Code = code,
            Timestamp = DateTime.UtcNow,
            TraceId = correlationId,
            Errors = errors,
            // RFC 7807 fields for backward compatibility
            Title = title,
            Status = statusCode,
            Detail = exception.Message
        };

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    /// <summary>
    /// Maps exception types to their corresponding HTTP status codes.
    /// 
    /// This method implements the exception-to-status-code mapping for the API.
    /// Follows RFC 7807 Problem Details format for consistency.
    /// 
    /// Mapping:
    /// - 400 Bad Request: General business logic errors, invalid HTTP requests
    /// - 403 Forbidden: Business rule authorization failures
    /// - 404 Not Found: Resource not found scenarios
    /// - 409 Conflict: Business rule conflicts (duplicates, state violations)
    /// - 422 Unprocessable Entity: Validation errors, domain rule violations
    /// - 500 Internal Server Error: Unhandled exceptions
    /// </summary>
    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            // Validation errors - client did not provide valid data
            ValidationAppException => StatusCodes.Status422UnprocessableEntity,
            ValidationException => StatusCodes.Status422UnprocessableEntity,
            
            // Domain exceptions - business rule violations
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status403Forbidden,
            InvalidStateException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status422UnprocessableEntity,
            
            // Legacy exceptions - backward compatibility
            BusinessLogicException => StatusCodes.Status400BadRequest,
            
            // Framework exceptions - HTTP protocol violations
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            
            // Unhandled exceptions - unexpected server errors
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>
    /// Maps exception types to machine-readable error codes.
    /// 
    /// These codes enable programmatic error handling in clients.
    /// </summary>
    private static string? GetErrorCode(Exception exception) =>
        exception switch
        {
            ValidationAppException => ErrorCodes.ValidationError,
            ValidationException => ErrorCodes.ValidationError,
            NotFoundException => ErrorCodes.ResourceNotFound,
            ConflictException => ErrorCodes.ConflictError,
            UnauthorizedException => ErrorCodes.UnauthorizedError,
            InvalidStateException => ErrorCodes.InvalidStateError,
            BusinessLogicException => ErrorCodes.BusinessLogicError,
            BadHttpRequestException => ErrorCodes.ValidationError,
            KeyNotFoundException => ErrorCodes.ResourceNotFound,
            DomainException => ErrorCodes.BusinessLogicError,
            _ => ErrorCodes.InternalServerError
        };

    /// <summary>
    /// Gets a human-readable title for the error response.
    /// 
    /// The title provides a brief, stable description of the error type that can be
    /// used by API consumers for user-friendly error messages or error categorization.
    /// </summary>
    private static string GetTitle(Exception exception) =>
        exception switch
        {
            ValidationAppException => "Validation Error",
            ValidationException => "Validation Error",
            NotFoundException => "Not Found",
            ConflictException => "Conflict",
            UnauthorizedException => "Forbidden",
            InvalidStateException => "Invalid State",
            DomainException => "Domain Error",
            BusinessLogicException => "Business Rule Violation",
            BadHttpRequestException => "Bad Request",
            KeyNotFoundException => "Not Found",
            _ => "Server Error"
        };

    /// <summary>
    /// Extracts field-level error details from specific exception types.
    /// 
    /// Returns field-level error details for validation and domain exceptions.
    /// For other exception types, returns null (no field-level details).
    /// 
    /// This allows API consumers to map errors to specific form fields
    /// or API request properties that caused the error.
    /// </summary>
    private static IReadOnlyDictionary<string, string[]>? GetErrors(Exception exception)
    {
        return exception switch
        {
            ValidationAppException validationException => validationException.Errors,
            DomainException domainException => domainException.Details,
            _ => null
        };
    }
}
