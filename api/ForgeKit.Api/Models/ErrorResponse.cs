namespace ForgeKit.Api.Models;

/// <summary>
/// Standardized error response returned by API for all error scenarios.
/// 
/// Based on RFC 7807 Problem Details format for consistency with HTTP standards.
/// Provides machine-readable error codes, field-level validation details, and trace IDs
/// for correlation with server logs.
/// 
/// The response format supports:
/// - Validation errors with field-level details
/// - Business logic errors with specific error codes
/// - Request tracing via TraceId for debugging production issues
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// User-friendly error message describing what went wrong.
    /// This is the primary message shown to end users.
    /// </summary>
    /// Example: Validation failed.
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code for programmatic client error handling.
    /// Enables clients to determine how to handle errors (retry logic, user messaging, etc.).
    /// 
    /// Standard codes:
    /// - VALIDATION_ERROR (422): Input validation failed
    /// - RESOURCE_NOT_FOUND (404): Resource doesn't exist
    /// - CONFLICT_ERROR (409): Resource conflict or duplicate
    /// - UNAUTHORIZED_ERROR (403): Access denied
    /// - BUSINESS_LOGIC_ERROR (400): Business rule violated
    /// - INVALID_STATE_ERROR (400): Invalid entity state
    /// - INTERNAL_SERVER_ERROR (500): Unhandled server error
    /// </summary>
    /// Example: VALIDATION_ERROR.
    public string? Code { get; set; }

    /// <summary>
    /// UTC timestamp when the error occurred.
    /// Useful for logging, auditing, and correlating with server events.
    /// </summary>
    /// Example: 2026-02-09T12:00:00Z.
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Unique identifier for tracing and correlating related logs and requests.
    /// 
    /// The trace ID enables distributed tracing in microservices environments.
    /// Clients can use this ID to correlate error responses with server logs,
    /// enabling faster debugging of production issues.
    /// 
    /// Generated from:
    /// - X-Correlation-ID header if provided by client
    /// - HttpContext.TraceIdentifier if not provided
    /// </summary>
    /// Example: 0HN1GG2P6JFBM:00000001.
    public string? TraceId { get; set; }

    /// <summary>
    /// Field-level validation or business rule errors (optional).
    /// 
    /// Only populated for validation and domain errors with field-level details.
    /// Maps field names to arrays of error messages for that field.
    /// 
    /// Useful for:
    /// - Displaying inline form field errors in UI
    /// - Mapping validation failures to specific properties
    /// - Providing context about which fields are problematic
    /// 
    /// Example:
    /// {
    ///   "dueDate": ["Must be a future date"],
    ///   "name": ["Cannot be empty", "Minimum length is 3 characters"],
    ///   "workspaceId": ["Required"]
    /// }
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// RFC 7807 compliant title (for backward compatibility).
    /// A short, human-readable summary of the problem type.
    /// Examples: "Not Found", "Conflict", "Validation Error", "Forbidden"
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// HTTP status code (for RFC 7807 compliance and backward compatibility).
    /// Allows clients to understand the error category without parsing the message.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Human-readable explanation specific to this error occurrence (for RFC 7807 compliance).
    /// Typically the same as Message for this implementation.
    /// </summary>
    public string? Detail { get; set; }
}
