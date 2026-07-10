namespace ForgeKit.Api.Exceptions;

/// <summary>
/// Exception thrown when request validation fails due to invalid input data.
/// </summary>
/// <remarks>
/// This exception is thrown by FluentValidation when request data does not meet
/// validation rules. It contains field-level error details that are returned to
/// API consumers for correction.
/// 
/// Mapped to: HTTP 422 Unprocessable Entity
/// The Errors collection is returned in the error response for client-side correction.
/// </remarks>
public class ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurs")
{
    /// <summary>
    /// Field-level validation error details.
    /// Key is the property name, value is an array of error messages for that property.
    /// </summary>
    /// Example: { "email": ["Email is required", "Email format is invalid"], "age": ["Age must be positive"] }.
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
