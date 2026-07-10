namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when a resource is in an invalid state for the requested operation.
/// </summary>
/// <remarks>
/// Mapped to: HTTP 400 Bad Request with error code INVALID_STATE_ERROR.
/// </remarks>
public class InvalidStateException(string message, string? stateField = null)
    : DomainException(message, "INVALID_STATE")
{
    /// <summary>
    /// Optional field or state name that caused the invalid state error.
    /// </summary>
    public string? StateField { get; } = stateField;
}
