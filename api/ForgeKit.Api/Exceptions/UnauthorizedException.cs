namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when a business rule denies authorization for an operation.
/// 
/// This exception is distinct from authentication failures (no valid token).
/// It represents domain-level authorization checks where the user is authenticated
/// but lacks permission based on business rules or resource ownership.
/// 
/// Common scenarios:
/// - User lacks required permissions for an operation
/// - Cannot perform action due to resource state (e.g., cannot cancel past visit)
/// - Resource access is denied based on ownership or role
/// - Business constraint prevents the operation
/// 
/// Mapped to: HTTP 403 Forbidden
/// </summary>
public class UnauthorizedException(string message = "Unauthorized access") 
    : DomainException(message, "UNAUTHORIZED")
{
}
