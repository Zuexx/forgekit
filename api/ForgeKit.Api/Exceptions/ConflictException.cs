namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when an operation violates a business rule due to conflicting data or state.
/// 
/// This exception represents situations where the request cannot be processed because:
/// - A unique constraint is violated (duplicate email, duplicate identifier, etc.)
/// - The resource is in an invalid state for the requested operation
/// - A business rule prevents the operation due to existing data
/// 
/// Mapped to: HTTP 409 Conflict
/// </summary>
public class ConflictException(string message, string? conflictField = null) 
    : DomainException(message, "CONFLICT")
{
    /// <summary>
    /// Optional field name that caused the conflict.
    /// Useful for identifying which field has the duplicate or conflicting value.
    /// </summary>
    public string? ConflictField { get; } = conflictField;
}
