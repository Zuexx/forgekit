namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when a business logic validation fails.
/// </summary>
/// <remarks>
/// This exception is thrown when domain business rules are violated but the error
/// doesn't fit other specific domain exception types (NotFoundException, ConflictException, etc.).
/// 
/// Mapped to: HTTP 400 Bad Request
/// </remarks>
public class BusinessLogicException(string message) : Exception(message);

