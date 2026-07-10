namespace ForgeKit.Api.Interfaces;

/// <summary>
/// Interface for feature modules that encapsulate related endpoints and services.
/// </summary>
/// <remarks>
/// Modules follow the vertical slice pattern, grouping related handlers, services, and endpoints.
/// Each module is responsible for registering its own dependencies and endpoints.
/// 
/// Implementation steps:
/// 1. Create a class implementing IModule
/// 2. Implement RegisterModule to register handlers, services, and validators
/// 3. Implement MapEndpoints to configure HTTP endpoints
/// 4. The application automatically discovers and registers modules via reflection
/// 
/// Example:
/// <code>
/// public class MyModule : IModule
/// {
///     public IServiceCollection RegisterModule(IServiceCollection services)
///     {
///         // Register handlers, services, validators
///         return services;
///     }
///     
///     public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
///     {
///         // Map HTTP endpoints
///         return endpoints;
///     }
/// }
/// </code>
/// </remarks>
public interface IModule
{
    /// <summary>
    /// Registers the module's services, handlers, and validators with the DI container.
    /// </summary>
    /// <param name="services">The service collection to register with</param>
    /// <returns>The same service collection for chaining</returns>
    IServiceCollection RegisterModule(IServiceCollection services);

    /// <summary>
    /// Maps the module's HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <returns>The same endpoint route builder for chaining</returns>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}