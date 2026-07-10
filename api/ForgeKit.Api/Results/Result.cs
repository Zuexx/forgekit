namespace ForgeKit.Api.Results;

/// <summary>
/// Represents the result of an operation as either success or failure.
/// 
/// This discriminated union type enables explicit, type-safe error handling
/// for expected business logic outcomes without exceptions.
/// 
/// Usage:
/// - Return Success(data) for successful operations
/// - Return Failure(code, message) for expected business errors
/// - Throw exceptions for truly unexpected errors (caught by middleware)
/// </summary>
public abstract record Result<T>
{
    /// <summary>
    /// Represents a successful operation with resulting data.
    /// </summary>
    public sealed record Success(T Data) : Result<T>;

    /// <summary>
    /// Represents a failed operation with error information.
    /// </summary>
    public sealed record Failure(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null
    ) : Result<T>;
}
