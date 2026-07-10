using Serilog.Context;

namespace ForgeKit.Api.Middlewares;

/// <summary>
/// Middleware for injecting correlation IDs into the logging context.
/// </summary>
/// <remarks>
/// This middleware enables distributed request tracing across the application lifecycle.
/// It performs the following:
/// - Extracts correlation ID from X-Correlation-ID request header if present
/// - Generates a new GUID correlation ID if not provided by client
/// - Injects the correlation ID into Serilog logging context
/// - Adds the correlation ID to the response header
/// 
/// This allows all logs within the request lifecycle to be tagged with the same correlation ID,
/// enabling tracing of a single request across logs, storage, and third-party systems.
/// 
/// Usage in clients: Include X-Correlation-ID header in requests to track request chains.
/// </remarks>
public class CorrelationIdMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Processes the request and injects correlation ID into logging context.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Extract correlation ID from request header or generate a new one
        var correlationId = ExtractOrGenerateCorrelationId(context.Request.Headers);

        // Inject correlation ID into the logging context for all downstream logs
        using (LogContext.PushProperty(CorrelationIdHeader, correlationId))
        {
            // Add correlation ID to response headers
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            await next(context);
        }
    }

    /// <summary>
    /// Extracts correlation ID from request headers or generates a new GUID if not present.
    /// </summary>
    /// <param name="requestHeaders">The request headers</param>
    /// <returns>The correlation ID (from header or newly generated GUID)</returns>
    private static string ExtractOrGenerateCorrelationId(IHeaderDictionary requestHeaders)
    {
        if (requestHeaders.TryGetValue(CorrelationIdHeader, out var correlationIdValue) &&
            !string.IsNullOrWhiteSpace(correlationIdValue))
        {
            return correlationIdValue.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}

