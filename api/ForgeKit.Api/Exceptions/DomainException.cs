namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Base exception for domain-level business rule violations.
/// This abstract class provides a common foundation for all domain-specific exceptions,
/// enabling consistent error handling and categorization across the application.
/// 
/// Domain exceptions represent intentional, business-related errors that should be
/// communicated to API consumers. They are distinct from infrastructure errors
/// or unhandled exceptions.
/// </summary>
public abstract class DomainException(string message, string? code = null) : Exception(message)
{
    /// <summary>
    /// Machine-readable error code for consistent error classification.
    /// Examples: "NOT_FOUND", "CONFLICT", "UNAUTHORIZED", "INVALID_STATE"
    /// </summary>
    public string? Code { get; } = code;

    /// <summary>
    /// Optional field-level error details for validation or domain constraint violations.
    /// Key is the field name, value is an array of error messages.
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }
}
