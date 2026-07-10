namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// 
/// This exception should be thrown by domain services, repositories, or handlers
/// when an operation requires a resource that doesn't exist or has been deleted.
/// 
/// Mapped to: HTTP 404 Not Found
/// </summary>
public class NotFoundException(string message, string? resourceType = null) 
    : DomainException(message, "NOT_FOUND")
{
    /// <summary>
    /// Optional resource type identifier (e.g., "Patient", "Visit", "Order").
    /// Useful for generic error handling and logging.
    /// </summary>
    public string? ResourceType { get; } = resourceType;
}
