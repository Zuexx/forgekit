namespace ForgeKit.Api.Extensions;

/// <summary>
/// Extension methods for configuring CORS (Cross-Origin Resource Sharing) policies.
/// Provides environment-aware CORS configuration with security-first approach.
/// </summary>
public static class CorsExtensions
{
    private const string DefaultPolicyName = "DefaultCorsPolicy";
    
    /// <summary>
    /// Adds CORS policy with environment-aware configuration.
    /// Development: Allows any origin for ease of development.
    /// Production: Strict whitelist from appsettings.json.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Hosting environment</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when production environment has no allowed origins configured
    /// </exception>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        
        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    // Development: Allow any origin for local development
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    // Production: Strict whitelist for security
                    if (allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Production environment requires 'Cors:AllowedOrigins' configuration. " +
                            "Add allowed origins to appsettings.json.");
                    }
                    
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()  // Allow Authorization header and cookies
                          .SetPreflightMaxAge(TimeSpan.FromHours(1));  // Cache preflight for 1 hour
                }
            });
        });
        
        return services;
    }
    
    /// <summary>
    /// Enables CORS middleware with the default policy.
    /// MUST be called before UseAuthentication() in the middleware pipeline.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(DefaultPolicyName);
        return app;
    }
}
