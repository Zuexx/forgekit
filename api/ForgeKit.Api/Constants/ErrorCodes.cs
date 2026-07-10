namespace ForgeKit.Api.Constants;

/// <summary>
/// Standardized error codes for API responses.
/// 
/// These machine-readable error codes enable programmatic error handling in clients,
/// allowing them to determine how to handle errors (retry logic, user messaging, etc.).
/// 
/// Each code corresponds to a specific exception type and HTTP status code.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Validation error (HTTP 422 Unprocessable Entity).
    /// Thrown when input validation fails (e.g., required fields missing, invalid format).
    /// </summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>
    /// Resource not found (HTTP 404 Not Found).
    /// Thrown when a requested resource doesn't exist.
    /// </summary>
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    /// <summary>
    /// Conflict error (HTTP 409 Conflict).
    /// Thrown when a resource conflict occurs (e.g., duplicate entity, state mismatch).
    /// </summary>
    public const string ConflictError = "CONFLICT_ERROR";

    /// <summary>
    /// Unauthorized/Forbidden (HTTP 403 Forbidden).
    /// Thrown when a user lacks permission for an operation.
    /// </summary>
    public const string UnauthorizedError = "UNAUTHORIZED_ERROR";

    /// <summary>
    /// Business logic error (HTTP 400 Bad Request).
    /// Thrown when a business rule is violated.
    /// Example: Attempting to restore a soft-deleted entity beyond the grace period.
    /// </summary>
    public const string BusinessLogicError = "BUSINESS_LOGIC_ERROR";

    /// <summary>
    /// Invalid state error (HTTP 400 Bad Request).
    /// Thrown when an entity is in an invalid state for the requested operation.
    /// </summary>
    public const string InvalidStateError = "INVALID_STATE_ERROR";

    /// <summary>
    /// Internal server error (HTTP 500 Internal Server Error).
    /// Thrown when an unhandled exception occurs on the server.
    /// </summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
