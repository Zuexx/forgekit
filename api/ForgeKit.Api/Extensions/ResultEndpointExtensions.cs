using ForgeKit.Api.Results;
using Microsoft.AspNetCore.Http;
using AspNetResults = Microsoft.AspNetCore.Http.Results;

namespace ForgeKit.Api.Extensions;

/// <summary>
/// Utility for mapping Result error codes to HTTP status codes.
/// Universal pattern applicable to any domain with Minimal APIs.
/// </summary>
public static class ResultErrorCodeMapper
{
    /// <summary>
    /// Maps error codes to appropriate HTTP status codes following universal patterns.
    /// </summary>
    public static int MapToHttpStatus(string errorCode)
    {
        return errorCode switch
        {
            _ when errorCode.EndsWith("_NOT_FOUND") => StatusCodes.Status404NotFound,
            _ when errorCode.EndsWith("_CONFLICT") => StatusCodes.Status409Conflict,
            _ when errorCode.StartsWith("INVALID_") => StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR" => StatusCodes.Status422UnprocessableEntity,
            _ when errorCode.StartsWith("UNAUTHORIZED_") => StatusCodes.Status403Forbidden,
            "UNAUTHORIZED" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    /// <summary>
    /// Maps error code to RFC 7807 title/problem description.
    /// </summary>
    public static string MapToErrorTitle(string errorCode)
    {
        return errorCode switch
        {
            _ when errorCode.EndsWith("_NOT_FOUND") => "Not Found",
            _ when errorCode.EndsWith("_CONFLICT") => "Conflict",
            _ when errorCode.StartsWith("INVALID_") => "Validation Error",
            "VALIDATION_ERROR" => "Validation Error",
            _ when errorCode.StartsWith("UNAUTHORIZED_") => "Forbidden",
            "UNAUTHORIZED" => "Forbidden",
            _ => "Error"
        };
    }
}

/// <summary>
/// Extension methods for converting Result{T} to Minimal API IResult responses.
/// Universal pattern applicable to any domain endpoint.
/// </summary>
public static class ResultEndpointExtensions
{
    /// <summary>
    /// Converts a Result{T} to an appropriate IResult HTTP response.
    /// Success cases → 200 OK with data
    /// Failure cases → 4xx/5xx with RFC 7807 problem response
    /// </summary>
    public static IResult ToHttpResponse<T>(this Result<T> result)
    {
        return result switch
        {
            Result<T>.Success success =>
                AspNetResults.Ok(success.Data),

            Result<T>.Failure failure =>
                AspNetResults.Problem(
                    detail: failure.Message,
                    title: ResultErrorCodeMapper.MapToErrorTitle(failure.Code),
                    statusCode: ResultErrorCodeMapper.MapToHttpStatus(failure.Code),
                    extensions: failure.Details != null ?
                        new Dictionary<string, object?> { { "errors", failure.Details } } :
                        null
                ),

            _ => AspNetResults.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Converts a Result{List{T}} to an IResult for collection endpoints.
    /// </summary>
    public static IResult ToHttpResponse<T>(this Result<List<T>> result)
    {
        return result switch
        {
            Result<List<T>>.Success success =>
                AspNetResults.Ok(success.Data),

            Result<List<T>>.Failure failure =>
                AspNetResults.Problem(
                    detail: failure.Message,
                    title: ResultErrorCodeMapper.MapToErrorTitle(failure.Code),
                    statusCode: ResultErrorCodeMapper.MapToHttpStatus(failure.Code),
                    extensions: failure.Details != null ?
                        new Dictionary<string, object?> { { "errors", failure.Details } } :
                        null
                ),

            _ => AspNetResults.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
