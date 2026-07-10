using ForgeKit.Api.Exceptions;
using FluentValidation;
using MediatR;

namespace ForgeKit.Api.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates all requests before handling.
/// </summary>
/// <remarks>
/// This behavior is registered in the MediatR pipeline and automatically validates
/// all requests using registered FluentValidation validators.
/// 
/// If validation fails, throws ValidationAppException with field-level error details.
/// If no validators are registered for the request type, passes through to handler.
/// 
/// This enables centralized, declarative validation without code duplication in handlers.
/// </remarks>
/// <typeparam name="TRequest">The request type being validated</typeparam>
/// <typeparam name="TResponse">The response type from the handler</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    /// <summary>
    /// Validates the request before passing it to the handler.
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Handler response if validation passes</returns>
    /// <exception cref="ValidationAppException">Thrown if validation fails with field-level error details</exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var errorsDictionary = _validators
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x != null)
            .GroupBy(
                x => x.PropertyName,
                x => x.ErrorMessage,
                (propertyName, errorMessages) => new
                {
                    Key = propertyName,
                    Values = errorMessages.Distinct().ToArray()
                })
            .ToDictionary(x => x.Key, x => x.Values);

        if (errorsDictionary.Count > 0)
        {
            throw new ValidationAppException(errorsDictionary);
        }

        return await next(cancellationToken);
    }
}