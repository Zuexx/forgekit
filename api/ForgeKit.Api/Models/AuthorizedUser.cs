namespace ForgeKit.Api.Models;

/// <summary>
/// Represents an authorized user from JWT token claims.
/// </summary>
/// <remarks>
/// This model is extracted from JWT claims during authentication
/// and represents the authenticated user making the request.
/// </remarks>
public class AuthorizedUser
{
    /// <summary>
    /// The unique identifier of the user (from ClaimTypes.NameIdentifier).
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The display name of the user (from ClaimTypes.Name).
    /// </summary>
    public string Name { get; set; } = default!;
}