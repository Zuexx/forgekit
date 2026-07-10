namespace ForgeKit.Api.Results;

/// <summary>
/// Extension methods for Result{T} enabling functional composition and pattern matching.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Transforms the success value to a new type if successful, otherwise returns the failure.
    /// </summary>
    public static Result<TNext> Map<T, TNext>(
        this Result<T> result,
        Func<T, TNext> map)
    {
        return result switch
        {
            Result<T>.Success success => new Result<TNext>.Success(map(success.Data)),
            Result<T>.Failure failure => new Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }

    /// <summary>
    /// Chains result-returning operations (monadic bind).
    /// If successful, applies the bind function. If failed, returns the failure.
    /// </summary>
    public static Result<TNext> Bind<T, TNext>(
        this Result<T> result,
        Func<T, Result<TNext>> bind)
    {
        return result switch
        {
            Result<T>.Success success => bind(success.Data),
            Result<T>.Failure failure => new Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }

    /// <summary>
    /// Asynchronously chains result-returning operations (async monadic bind).
    /// If successful, applies the bind function. If failed, returns the failure.
    /// </summary>
    public static async Task<Result<TNext>> BindAsync<T, TNext>(
        this Result<T> result,
        Func<T, Task<Result<TNext>>> bind)
    {
        return result switch
        {
            Result<T>.Success success => await bind(success.Data),
            Result<T>.Failure failure => new Result<TNext>.Failure(failure.Code, failure.Message, failure.Details),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }

    /// <summary>
    /// Pattern matches on the result and executes the appropriate callback.
    /// </summary>
    public static TResult Match<T, TResult>(
        this Result<T> result,
        Func<T, TResult> onSuccess,
        Func<string, string, IReadOnlyDictionary<string, string[]>?, TResult> onFailure)
    {
        return result switch
        {
            Result<T>.Success success => onSuccess(success.Data),
            Result<T>.Failure failure => onFailure(failure.Code, failure.Message, failure.Details),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }

    /// <summary>
    /// Executes a side effect if successful, then returns the result unchanged.
    /// </summary>
    public static Result<T> OnSuccess<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (result is Result<T>.Success success)
        {
            action(success.Data);
        }

        return result;
    }

    /// <summary>
    /// Executes a side effect if failed, then returns the result unchanged.
    /// </summary>
    public static Result<T> OnFailure<T>(
        this Result<T> result,
        Action<string, string> action)
    {
        if (result is Result<T>.Failure failure)
        {
            action(failure.Code, failure.Message);
        }

        return result;
    }

    /// <summary>
    /// Returns the success data or throws an exception if failed.
    /// Use only for edge cases where you need to convert Result to exception.
    /// </summary>
    public static T GetValueOrThrow<T>(this Result<T> result, Func<string, string, IReadOnlyDictionary<string, string[]>?, Exception>? exceptionFactory = null)
    {
        return result switch
        {
            Result<T>.Success success => success.Data,
            Result<T>.Failure failure => throw (exceptionFactory?.Invoke(failure.Code, failure.Message, failure.Details)
                ?? new InvalidOperationException($"Result failed with code '{failure.Code}': {failure.Message}")),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }
}
