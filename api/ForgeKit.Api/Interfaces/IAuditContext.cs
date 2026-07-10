namespace ForgeKit.Api.Interfaces;

/// <summary>
/// Provides audit context information (user identity and timestamp) for the current request.
/// </summary>
/// <remarks>
/// This interface is registered as a scoped service, ensuring one instance per HTTP request.
/// It extracts user identity from JWT claims and provides a consistent audit context
/// throughout the request lifecycle.
/// </remarks>
public interface IAuditContext
{
    /// <summary>
    /// Gets the current user's ID from the JWT token's NameIdentifier claim.
    /// </summary>
    /// <remarks>
    /// Falls back to "system" if no authenticated user is present or the claim is missing.
    /// This allows non-authenticated requests (e.g., health checks) to be handled gracefully.
    /// </remarks>
    string UserId { get; }

    /// <summary>
    /// Gets the current user's name from the JWT token's Name claim.
    /// </summary>
    /// <remarks>
    /// Falls back to "system" if no authenticated user is present or the claim is missing.
    /// </remarks>
    string UserName { get; }

    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    /// <remarks>
    /// Used for populating audit fields like CreatedAt, UpdatedAt, and DeletedAt.
    /// Always returns DateTime.UtcNow to ensure consistency with database UTC timestamps.
    /// </remarks>
    DateTime UtcNow { get; }
}
