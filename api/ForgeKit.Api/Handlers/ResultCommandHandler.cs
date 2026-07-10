using ForgeKit.Api.Results;
using MediatR;

namespace ForgeKit.Api.Handlers;

/// <summary>
/// Abstract base class for command handlers returning Result{TResponse}.
/// 
/// Provides common pattern for handlers that may fail with business logic errors.
/// Handlers should override HandleAsync and return Result.Success or Result.Failure.
/// 
/// This replaces ICommandHandler which is no longer needed with Result Pattern.
/// </summary>
/// <typeparam name="TRequest">The command request type</typeparam>
/// <typeparam name="TResponse">The response data type</typeparam>
public abstract class ResultCommandHandler<TRequest, TResponse>(ILogger<ResultCommandHandler<TRequest, TResponse>> logger) : IRequestHandler<TRequest, Result<TResponse>>
    where TRequest : IRequest<Result<TResponse>>
{
    /// <summary>
    /// Logger instance for structured logging in handlers.
    /// Subclasses can access this to log business events.
    /// </summary>
    protected ILogger<ResultCommandHandler<TRequest, TResponse>> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Main handler method. Derived classes override this to implement command logic.
    /// </summary>
    public abstract Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Mediator entry point. Delegates to HandleAsync implementation with logging.
    /// </summary>
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var handlerName = this.GetType().Name;
        var requestName = typeof(TRequest).Name;

        try
        {
            Logger.LogDebug(
                "Executing {HandlerName} for {RequestName}",
                handlerName, requestName);

            var result = await HandleAsync(request, cancellationToken);

            if (result is Result<TResponse>.Success)
            {
                Logger.LogInformation(
                    "{HandlerName} completed successfully for {RequestName}",
                    handlerName, requestName);
            }
            else if (result is Result<TResponse>.Failure failure)
            {
                Logger.LogWarning(
                    "{HandlerName} returned failure for {RequestName}: Code={ErrorCode}, Message={ErrorMessage}",
                    handlerName, requestName, failure.Code, failure.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex, "{HandlerName} threw an exception while handling {RequestName}",
                handlerName, requestName);
            throw;
        }
    }

    /// <summary>
    /// Helper method to create a not found failure result with standard error code.
    /// </summary>
    public static Result<TResponse> NotFound(string message, string? resourceType = null)
    {
        var code = resourceType != null ? $"{resourceType.ToUpperInvariant()}_NOT_FOUND" : "RESOURCE_NOT_FOUND";
        return new Result<TResponse>.Failure(code, message);
    }

    /// <summary>
    /// Helper method to create a conflict failure result with standard error code.
    /// </summary>
    public static Result<TResponse> Conflict(string message, string? field = null)
    {
        var code = field != null ? $"{field.ToUpperInvariant()}_CONFLICT" : "CONFLICT";
        return new Result<TResponse>.Failure(code, message);
    }

    /// <summary>
    /// Helper method to create an unauthorized failure result with standard error code.
    /// </summary>
    public static Result<TResponse> Unauthorized(string message, string? operation = null)
    {
        var code = operation != null ? $"UNAUTHORIZED_{operation.ToUpperInvariant()}" : "UNAUTHORIZED";
        return new Result<TResponse>.Failure(code, message);
    }

    /// <summary>
    /// Helper method to create a validation error failure result with field-level details.
    /// </summary>
    public static Result<TResponse> ValidationError(string message, IReadOnlyDictionary<string, string[]> errors)
    {
        return new Result<TResponse>.Failure("VALIDATION_ERROR", message, errors);
    }

    /// <summary>
    /// Helper method to create a success result.
    /// </summary>
    public static Result<TResponse> Success(TResponse data)
    {
        return new Result<TResponse>.Success(data);
    }
}
