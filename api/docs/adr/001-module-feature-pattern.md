# ADR-001: Module/Feature Pattern for Service Organization

**Date:** 2026-02-09
**Status:** Accepted
**Author:** ForgeKit Architecture Team
**Supersedes:** None
**Related:** ADR-003, ADR-004
**Updated:** 2026-02-10 - Added ISampleModule pattern for environment-aware modules

## Context

Large .NET applications require a scalable approach to organizing related functionality. Traditional approaches either scatter features across layers (making it hard to locate related code) or create monolithic feature modules that become unwieldy as the application grows.

ForgeKit's architecture must support:
- Clear ownership of features and their dependencies
- Easy discovery of new features without manual registration
- Independent feature lifecycle management
- Composable feature modules that can be enabled/disabled based on configuration
- Testability of features in isolation
- Scalability to dozens of features without administrative burden
- **Sample/demo endpoints that are only available in development/testing environments**

### Current State
Without a module pattern, features tend to accumulate in service layers and controllers, creating:
- Difficulty locating all code related to a feature
- Implicit dependencies between features
- Manual registration of routes and services
- Tests scattered across test projects without clear organization

## Decision

Implement a **Module/Feature Pattern** where:

1. **Each logical feature is encapsulated in a Module** that implements `IModule` interface
2. **Modules are discovered via reflection** in `ModuleExtensions.cs` at application startup
3. **Modules handle both DI registration and endpoint mapping** through interface methods
4. **Modules can be organized hierarchically** under a `Modules/` directory structure
5. **Optional: Modules can be selectively enabled/disabled** via configuration
6. **Sample modules can be marked with `ISampleModule`** to be automatically excluded in production

### IModule Interface

```csharp
namespace Api.Interfaces;

public interface IModule
{
    /// <summary>
    /// Register module-specific services in the DI container.
    /// Called once at application startup.
    /// </summary>
    IServiceCollection RegisterModule(IServiceCollection services);

    /// <summary>
    /// Map module-specific endpoint routes.
    /// Called once at application startup after services are configured.
    /// </summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

### ISampleModule Marker Interface (NEW)

For demo/example endpoints that should only exist in non-production environments:

```csharp
namespace Api.Interfaces;

/// <summary>
/// Marker interface for sample/demo modules that should only be available in non-production environments.
/// Modules implementing this interface will be automatically excluded when IsProduction() == true.
/// </summary>
public interface ISampleModule : IModule
{
}
```

**Implementation in ModuleExtensions.cs:**
```csharp
public static WebApplication MapEndpoints(this WebApplication app)
{
    var endpoints = app.MapGroup("/v1");

    // Filter out sample modules in production
    var modulesToMap = app.Environment.IsProduction()
        ? registeredModules.Where(m => m is not ISampleModule)
        : registeredModules;

    foreach (var module in modulesToMap)
    {
        module.MapEndpoints(endpoints);
    }
    return app;
}
```

### Reflection-Based Discovery

```csharp
// ModuleExtensions.cs
private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.IsClass && p.IsAssignableTo(typeof(IModule)))
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

## Rationale

### Why Module Pattern?

1. **Vertical Slicing of Features:** Modules organize all code related to a feature vertically (UI, services, data access, tests) rather than horizontally by layer

2. **Self-Contained Units:** Each module is responsible for:
    - Registering its services
    - Configuring its routes
    - Managing its dependencies
    - This enables features to evolve independently

3. **Zero-Configuration Discovery:** Reflection-based module discovery eliminates:
    - Manual registration lists (error-prone, easy to forget)
    - Framework configuration files for features
    - Developers adding modules to wrong locations
    - Every developer registering features differently

4. **Scalability:** The pattern scales linearly:
    - 5 modules: Same effort as 2 modules to add a new one
    - 50 modules: Still same effort (just create new class, implement interface, done)
    - No central registry to maintain
    - No growing configuration files

5. **Testability:** Each module can be:
    - Tested in isolation by creating a minimal WebApplication with only that module
    - Tested with other specific modules without loading all modules
    - Unit tested by calling RegisterModule/MapEndpoints directly

6. **Team Scalability:** Multiple teams can work on different modules without:
    - Merge conflicts in registration code
    - Configuration file battles
    - Coordinating module names or ordering

### Why ISampleModule Marker?

Sample/demo endpoints like `/weatherforecast` serve important purposes but shouldn't be in production:
- **Development:** Help developers understand the API patterns and test middleware
- **Testing:** Provide reliable endpoints for integration tests without database dependencies
- **Documentation:** Serve as examples in API documentation
- **Production:** Should be excluded for security and clarity

Using `ISampleModule` ensures:
- Clear intent that a module is for demonstration
- Automatic exclusion in production (no accidental exposure)
- Available in all non-production environments (Development, Testing, Staging)
- No hardcoded endpoint filtering needed per module

## Alternatives Considered

### 1. No Module Pattern (Service Layer Organization)
**Approach:** Organize code into Controllers/Services/Repositories without explicit modules

**Pros:**
- Simple for small applications
- Minimal abstraction overhead
- Direct clarity about request flow

**Cons:**
- Doesn't scale beyond ~10 services
- Features become harder to locate as codebase grows
- No encapsulation of feature dependencies
- Difficult to onboard new developers to understand feature scope
- Manual registration of services creates bottlenecks

**When Better:** Micro-applications (< 3 features)

---

### 2. Manual Module Registration
**Approach:** Create IModule interface but require explicit registration in Program.cs

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);
builder.Services.RegisterModule<VisitsModule>();
builder.Services.RegisterModule<SampleResourceModule>();
builder.Services.RegisterModule<ComplianceModule>();
```

**Pros:**
- Explicit, easy to see what's loaded
- Can order modules intentionally
- Easy to selectively enable/disable modules

**Cons:**
- Developers forget to register new modules (especially when adding from templates)
- Central registration point becomes a bottleneck and merge conflict location
- Each module addition requires code change in Program.cs
- Easy to register same module twice by mistake
- Doesn't scale to 50+ modules (registration list becomes hard to manage)

**Trade-off:** Explicit over implicit, but loses scalability benefit

**When Better:** Small teams where developers are well-coordinated; < 15 modules

---

### 3. Convention-Based Module Discovery with Naming
**Approach:** Reflect to find types matching pattern like `*Module` in specific namespace

```csharp
private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.IsClass &&
                    p.Name.EndsWith("Module") &&
                    p.IsAssignableTo(typeof(IModule)))
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

**Pros:**
- Automatic discovery with clear naming convention
- Prevents accidental implementations of IModule from being discovered

**Cons:**
- Requires discipline on naming (easily broken)
- Adds hidden coupling to naming conventions
- Harder to debug if convention is violated
- Tests or utilities might accidentally match the pattern

**Trade-off:** Slightly safer than pure interface discovery

**When Better:** Teams with inconsistent code style or need stricter conventions

---

### 4. Attribute-Based Module Discovery
**Approach:** Create custom attribute and discover via reflection

```csharp
[ModuleDiscoverable]
public class VisitsModule : IModule { }

private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.GetCustomAttribute<ModuleDiscoverableAttribute>() != null)
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

**Pros:**
- Explicit marking of which classes are modules
- Prevents accidental discoveries
- Can include metadata in attribute (enabled/disabled, priority)
- Good for selective enabling/disabling

**Cons:**
- Additional boilerplate per module (attribute required)
- More complex discovery logic
- Harder to discover all modules (need to explain attribute)
- Overkill for simpler scenarios

**When Better:** Modules need selective enabling/disabling; advanced configuration needed

---

### 5. Sample Endpoint Handling

**Alternative A: Hardcoded environment checks in each sample module**
```csharp
public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var app = endpoints as WebApplication;
    if (app?.Environment.IsProduction() == true)
        return endpoints; // Skip in production

    // Register endpoints...
}
```

**Problems:** Repeated in every sample module, easy to forget, not clear intent

**Alternative B: ISampleModule Marker (CHOSEN)**
Uses interface inheritance to mark modules as sample, filtered once in ModuleExtensions

**Advantages:**
- Single filtering logic (DRY)
- Clear intent
- No repetition across modules
- Easy to audit all sample modules (find `ISampleModule` implementations)

## Consequences

### Positive

1. **Reduced Cognitive Load:** Developers working on a feature find all related code in one place
2. **Faster Onboarding:** New developers can understand a feature by reading one module
3. **Zero Overhead for Growth:** Adding new features requires no changes to existing code
4. **Natural Testing Structure:** Each module has corresponding test module
5. **Clear Ownership:** Easy to assign module owners
6. **Horizontal Scalability:** Can work without central coordination as team grows
7. **Feature Toggle Ready:** Modules can be conditionally registered based on configuration
8. **Sample Module Safety:** Demo endpoints automatically excluded from production

### Negative

1. **Hidden Dependency Discovery:** Developers might not realize all modules are automatically loaded (can be mitigated with documentation)
2. **Naming Conflicts:** Two developers might create modules with same name in different namespaces (use consistent naming convention)
3. **Module Ordering Issues:** If Module-A depends on Module-B registrations, discovery order matters (make modules independent or document ordering)
4. **Reflection Performance:** Startup time slightly increases due to reflection (minimal, typically < 10ms even with 50 modules)
5. **Less Explicit API:** Compared to manual registration, it's less obvious what modules are registered (excellent candidate for startup logging)

### Neutral

1. **Architectural Complexity:** Adds abstraction layer but improves overall system clarity
2. **Framework Lock-in:** Couples modules to IModule interface (but interface is minimal, easy to extract)

## When to Use

✅ **Use Module Pattern when:**
- Application has 3+ distinct features/domains
- Team size > 1 (especially important for teams > 3)
- Expected to grow (more than 10 features anticipated)
- Features have different release cycles
- Want clear feature ownership
- Want to reduce merge conflicts from shared infrastructure files
- Features might be conditionally enabled based on environment/subscription tier
- Need clear testing boundaries at feature level

✅ **Specifically for:**
- Feature domains (Visits, Compliance, Analytics, etc.)
- Cross-cutting concerns that behave like features (Notifications, Reporting)

✅ **Use ISampleModule for:**
- Demo/example endpoints used in development/testing
- Sample implementations for developers to learn from
- Endpoints that test framework features (middleware, error handling)
- Any feature that should be disabled in production

## When NOT to Use

❌ **Avoid Module Pattern when:**
- Application has only 1-2 features (simple CRUD service)
- Lightweight utility application (health checks, metrics collectors)
- Proof of concept or throwaway code
- Team is extremely small (1 person) and coordination isn't a concern
- Need absolute certainty about module load order at startup

❌ **Don't use for:**
- Infrastructure components (middleware, configuration, identity)
- Generic utilities (extension methods, helpers)
- Global cross-cutting concerns better handled by middleware

❌ **Don't mark as ISampleModule:**
- Production business features
- Features that should persist across environments
- Public API endpoints

## ForgeKit Implementation

### Directory Structure

```
ForgeKit.Api/
├── Modules/
│   ├── SampleResourceModule.cs (implements ISampleModule)
│   ├── Visits/
│   │   ├── VisitsModule.cs
│   │   ├── Handlers/
│   │   ├── Validators/
│   │   └── Models/
│   ├── Compliance/
│   │   ├── ComplianceModule.cs
│   │   ├── Handlers/
│   │   └── Models/
│   └── Analytics/
│       ├── AnalyticsModule.cs
│       └── ...
├── Interfaces/
│   ├── IModule.cs
│   └── ISampleModule.cs (NEW)
├── Extensions/
│   └── ModuleExtensions.cs
└── Program.cs
```

### Sample Module Implementation (with ISampleModule)

```csharp
// ForgeKit.Api/Modules/SampleResourceModule.cs
using Api.Interfaces;
using Api.Samples;
using Api.Handlers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Modules;

/// <summary>
/// Sample Resource Module for demonstration purposes.
/// This module is only available in non-production environments (Development, Testing, Staging).
/// It demonstrates module pattern and provides a simple endpoint for testing middleware.
/// </summary>
public class SampleResourceModule : ISampleModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Register handlers (MediatR auto-discovers them)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(SampleResourceModule).Assembly));

        // Register validators
        services.AddFluentValidation(typeof(SampleResourceModule).Assembly);

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resources").WithName("Resources").WithOpenApi();

        group.MapGet("", ListResources)
            .WithName("ListResources")
            .WithOpenApi()
            .Produces<List<ResourceDto>>(StatusCodes.Status200OK);

        group.MapGet("{id}", GetResourceById)
            .WithName("GetResourceById")
            .WithOpenApi()
            .Produces<ResourceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateResource)
            .WithName("CreateResource")
            .WithOpenApi()
            .Produces<ResourceDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("{id}", DeleteResource)
            .WithName("DeleteResource")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    // ... handler methods ...
}
```

### Key Characteristics

1. **Self-Registration:** Module knows its own services and endpoints
2. **No Central List:** No need to update Program.cs or configuration files
3. **Encapsulation:** All sample resource code lives under one module
4. **Environment-Aware:** Automatically excluded in Production
5. **Testability:** Can test module in isolation or with other modules

### Production Safety

When deployed to production (`IsProduction() == true`):
- `SampleResourceModule` is discovered but NOT mapped to endpoints
- `/v1/resources` endpoint is NOT available
- No configuration files needed
- Safe by default

## Testing Examples

### Unit Test Module Registration
```csharp
[Test]
public void SampleResourceModule_RegistersAllRequiredServices()
{
    var services = new ServiceCollection();
    var module = new SampleResourceModule();

    var result = module.RegisterModule(services);

    Assert.That(result, Is.Not.Null);
    var serviceProvider = result.BuildServiceProvider();

    Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IMediator>());
}
```

### Integration Test with TestWebApplicationFactory
```csharp
[Test]
public async Task SampleResourceModule_Endpoints_AvailableInTests()
{
    var factory = new TestWebApplicationFactory(); // Sets environment to Testing
    var client = factory.CreateClient();

    var response = await client.GetAsync("/v1/resources");

    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
```

### Verify Excluded in Production
```csharp
[Test]
public void SampleResourceModule_NotMappedInProduction()
{
    // Create a production environment builder
    var builder = WebApplication.CreateBuilder();
    builder.Environment.EnvironmentName = "Production";

    // Build app and verify sample endpoints aren't mapped
    var app = builder.Build();
    var sampleModule = new SampleResourceModule();

    sampleModule.RegisterModule(builder.Services);

    // In production, MapEndpoints would skip ISampleModule
    var moduleExtensions = typeof(ModuleExtensions);
    var mapEndpointsMethod = moduleExtensions.GetMethod("MapEndpoints");
    // Verify ISampleModule implementations are filtered out
}
```

## Related ADRs

- **ADR-003:** Validation in MediatR pipeline (used by modules)
- **ADR-004:** Direct DbContext access in services (used within module handlers)
- **ADR-005:** Automatic audit context (used across all modules)

## References

- [Modular Monolith Architecture Patterns](https://www.milanjovanovic.tech/blog/modular-monolith)
- [Feature Modules in .NET](https://github.com/ardalis/CleanArchitecture)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)

## Decision

Implement a **Module/Feature Pattern** where:

1. **Each logical feature is encapsulated in a Module** that implements `IModule` interface
2. **Modules are discovered via reflection** in `ModuleExtensions.cs` at application startup
3. **Modules handle both DI registration and endpoint mapping** through interface methods
4. **Modules can be organized hierarchically** under a `Modules/` directory structure
5. **Optional: Modules can be selectively enabled/disabled** via configuration

### IModule Interface

```csharp
namespace Api.Interfaces;

public interface IModule
{
    /// <summary>
    /// Register module-specific services in the DI container.
    /// Called once at application startup.
    /// </summary>
    IServiceCollection RegisterModule(IServiceCollection services);

    /// <summary>
    /// Map module-specific endpoint routes.
    /// Called once at application startup after services are configured.
    /// </summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

### Reflection-Based Discovery

```csharp
// ModuleExtensions.cs
private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.IsClass && p.IsAssignableTo(typeof(IModule)))
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

## Rationale

### Why Module Pattern?

1. **Vertical Slicing of Features:** Modules organize all code related to a feature vertically (UI, services, data access, tests) rather than horizontally by layer

2. **Self-Contained Units:** Each module is responsible for:
   - Registering its services
   - Configuring its routes
   - Managing its dependencies
   - This enables features to evolve independently

3. **Zero-Configuration Discovery:** Reflection-based module discovery eliminates:
   - Manual registration lists (error-prone, easy to forget)
   - Framework configuration files for features
   - Developers adding modules to wrong locations
   - Every developer registering features differently

4. **Scalability:** The pattern scales linearly:
   - 5 modules: Same effort as 2 modules to add a new one
   - 50 modules: Still same effort (just create new class, implement interface, done)
   - No central registry to maintain
   - No growing configuration files

5. **Testability:** Each module can be:
   - Tested in isolation by creating a minimal WebApplication with only that module
   - Tested with other specific modules without loading all modules
   - Unit tested by calling RegisterModule/MapEndpoints directly

6. **Team Scalability:** Multiple teams can work on different modules without:
   - Merge conflicts in registration code
   - Configuration file battles
   - Coordinating module names or ordering

## Alternatives Considered

### 1. No Module Pattern (Service Layer Organization)
**Approach:** Organize code into Controllers/Services/Repositories without explicit modules

**Pros:**
- Simple for small applications
- Minimal abstraction overhead
- Direct clarity about request flow

**Cons:**
- Doesn't scale beyond ~10 services
- Features become harder to locate as codebase grows
- No encapsulation of feature dependencies
- Difficult to onboard new developers to understand feature scope
- Manual registration of services creates bottlenecks

**When Better:** Micro-applications (< 3 features)

---

### 2. Manual Module Registration
**Approach:** Create IModule interface but require explicit registration in Program.cs

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);
builder.Services.RegisterModule<VisitsModule>();
builder.Services.RegisterModule<SampleResourceModule>();
builder.Services.RegisterModule<ComplianceModule>();
```

**Pros:**
- Explicit, easy to see what's loaded
- Can order modules intentionally
- Easy to selectively enable/disable modules

**Cons:**
- Developers forget to register new modules (especially when adding from templates)
- Central registration point becomes a bottleneck and merge conflict location
- Each module addition requires code change in Program.cs
- Easy to register same module twice by mistake
- Doesn't scale to 50+ modules (registration list becomes hard to manage)

**Trade-off:** Explicit over implicit, but loses scalability benefit

**When Better:** Small teams where developers are well-coordinated; < 15 modules

---

### 3. Convention-Based Module Discovery with Naming
**Approach:** Reflect to find types matching pattern like `*Module` in specific namespace

```csharp
private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.IsClass &&
                    p.Name.EndsWith("Module") &&
                    p.IsAssignableTo(typeof(IModule)))
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

**Pros:**
- Automatic discovery with clear naming convention
- Prevents accidental implementations of IModule from being discovered

**Cons:**
- Requires discipline on naming (easily broken)
- Adds hidden coupling to naming conventions
- Harder to debug if convention is violated
- Tests or utilities might accidentally match the pattern

**Trade-off:** Slightly safer than pure interface discovery

**When Better:** Teams with inconsistent code style or need stricter conventions

---

### 4. Attribute-Based Module Discovery
**Approach:** Create custom attribute and discover via reflection

```csharp
[ModuleDiscoverable]
public class VisitsModule : IModule { }

private static IEnumerable<IModule> DiscoverModules()
{
    return typeof(IModule).Assembly
        .GetTypes()
        .Where(p => p.GetCustomAttribute<ModuleDiscoverableAttribute>() != null)
        .Select(Activator.CreateInstance)
        .Cast<IModule>();
}
```

**Pros:**
- Explicit marking of which classes are modules
- Prevents accidental discoveries
- Can include metadata in attribute (enabled/disabled, priority)
- Good for selective enabling/disabling

**Cons:**
- Additional boilerplate per module (attribute required)
- More complex discovery logic
- Harder to discover all modules (need to explain attribute)
- Overkill for simpler scenarios

**When Better:** Modules need selective enabling/disabling; advanced configuration needed

---

## Consequences

### Positive

1. **Reduced Cognitive Load:** Developers working on a feature find all related code in one place
2. **Faster Onboarding:** New developers can understand a feature by reading one module
3. **Zero Overhead for Growth:** Adding new features requires no changes to existing code
4. **Natural Testing Structure:** Each module has corresponding test module
5. **Clear Ownership:** Easy to assign module owners
6. **Horizontal Scalability:** Can work without central coordination as team grows
7. **Feature Toggle Ready:** Modules can be conditionally registered based on configuration

### Negative

1. **Hidden Dependency Discovery:** Developers might not realize all modules are automatically loaded (can be mitigated with documentation)
2. **Naming Conflicts:** Two developers might create modules with same name in different namespaces (use consistent naming convention)
3. **Module Ordering Issues:** If Module-A depends on Module-B registrations, discovery order matters (make modules independent or document ordering)
4. **Reflection Performance:** Startup time slightly increases due to reflection (minimal, typically < 10ms even with 50 modules)
5. **Less Explicit API:** Compared to manual registration, it's less obvious what modules are registered (excellent candidate for startup logging)

### Neutral

1. **Architectural Complexity:** Adds abstraction layer but improves overall system clarity
2. **Framework Lock-in:** Couples modules to IModule interface (but interface is minimal, easy to extract)

## When to Use

✅ **Use Module Pattern when:**
- Application has 3+ distinct features/domains
- Team size > 1 (especially important for teams > 3)
- Expected to grow (more than 10 features anticipated)
- Features have different release cycles
- Want clear feature ownership
- Want to reduce merge conflicts from shared infrastructure files
- Features might be conditionally enabled based on environment/subscription tier
- Need clear testing boundaries at feature level

✅ **Specifically for:**
- Feature domains (Visits, Compliance, Analytics, etc.)
- Cross-cutting concerns that behave like features (Notifications, Reporting)

## When NOT to Use

❌ **Avoid Module Pattern when:**
- Application has only 1-2 features (simple CRUD service)
- Lightweight utility application (health checks, metrics collectors)
- Proof of concept or throwaway code
- Team is extremely small (1 person) and coordination isn't a concern
- Need absolute certainty about module load order at startup

❌ **Don't use for:**
- Infrastructure components (middleware, configuration, identity)
- Generic utilities (extension methods, helpers)
- Global cross-cutting concerns better handled by middleware

## ForgeKit Implementation

### Directory Structure

```
ForgeKit.Api/
├── Modules/
│   ├── SampleResourceModule.cs
│   ├── Visits/
│   │   ├── VisitsModule.cs
│   │   ├── Handlers/
│   │   ├── Validators/
│   │   └── Models/
│   ├── Compliance/
│   │   ├── ComplianceModule.cs
│   │   ├── Handlers/
│   │   └── Models/
│   └── Analytics/
│       ├── AnalyticsModule.cs
│       └── ...
├── Interfaces/
│   └── IModule.cs
├── Extensions/
│   └── ModuleExtensions.cs
└── Program.cs
```

### Sample Module Implementation

```csharp
// ForgeKit.Api/Modules/SampleResourceModule.cs
using Api.Interfaces;
using Api.Samples;
using Api.Handlers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Modules;

/// <summary>
/// Module for sample resource operations (list, get, create, delete).
/// Demonstrates module pattern for a complete feature.
/// </summary>
public class SampleResourceModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Register handlers (though MediatR auto-discovers them)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(SampleResourceModule).Assembly));

        // Register validators
        services.AddFluentValidation(typeof(SampleResourceModule).Assembly);

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/samples").WithName("Samples").WithOpenApi();

        group.MapGet("", ListResources)
            .WithName("ListSamples")
            .WithOpenApi()
            .Produces<List<SampleResourceDto>>(StatusCodes.Status200OK);

        group.MapGet("{id}", GetResourceById)
            .WithName("GetSample")
            .WithOpenApi()
            .Produces<SampleResourceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateResource)
            .WithName("CreateSample")
            .WithOpenApi()
            .Produces<SampleResourceDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("{id}", DeleteResource)
            .WithName("DeleteSample")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private async Task<IResult> ListResources(
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new SampleListResourcesQuery(), ct);
        return Results.Ok(result);
    }

    private async Task<IResult> GetResourceById(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new SampleGetResourceByIdQuery(id), ct);
        return Results.Ok(result);
    }

    private async Task<IResult> CreateResource(
        CreateSampleResourceRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new SampleCreateResourceCommand(request.Name, request.Description),
            ct);
        return Results.Created($"/v1/samples/{result.Id}", result);
    }

    private async Task<IResult> DeleteResource(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new SampleDeleteResourceCommand(id), ct);
        return Results.NoContent();
    }
}
```

### Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.RegisterModules(); // Discovers and registers all IModule implementations

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app
    .MapEndpoints() // Maps endpoints from all modules
    .StartSeed();

app.Run();
```

### Key Characteristics

1. **Self-Registration:** Module knows its own services and endpoints
2. **No Central List:** No need to update Program.cs or configuration files
3. **Encapsulation:** All sample resource code lives under one module
4. **Testability:** Can test VisitsModule in isolation by:
   ```csharp
   [Test]
   public void VisitsModule_RegistersRequiredServices()
   {
       var services = new ServiceCollection();
       var module = new VisitsModule();

       var result = module.RegisterModule(services);

       Assert.That(result, Is.Not.Null);
       // Assert specific services are registered
   }
   ```

## Considerations for ForgeKit

### Module Organization
- Group related features: `Visits` module contains creation, approval, soft-delete
- Don't create modules for single handlers: Minimum feature scope is ~3-5 endpoints
- Cross-cutting modules: `Common` or `Core` for shared utilities (if needed)

### Dependency Management
- Modules should depend on shared interfaces (IAuditContext, IUnitOfWork) not other modules
- If Module-A needs Module-B's services, reconsider the module boundaries
- Document external dependencies in module XML comments

### Selective Enabling
For future subscription-based tiers:
```csharp
public IServiceCollection RegisterModule(IServiceCollection services)
{
    var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
    var enabledFeatures = config.GetSection("Features").Get<Dictionary<string, bool>>();

    if (enabledFeatures?.GetValueOrDefault("Compliance") != true)
        return services; // Skip registration

    // ... normal registration
}
```

## Testing Examples

### Unit Test Module Registration
```csharp
[Test]
public void SampleResourceModule_RegistersAllRequiredServices()
{
    var services = new ServiceCollection();
    var module = new SampleResourceModule();

    var result = module.RegisterModule(services);

    Assert.That(result, Is.Not.Null);
    var serviceProvider = result.BuildServiceProvider();

    Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IMediator>());
}
```

### Integration Test Module Endpoints
```csharp
[Test]
public async Task SampleResourceModule_Maps_ListEndpoint()
{
    var builder = WebApplicationBuilder.CreateBuilder();
    var module = new SampleResourceModule();

    module.RegisterModule(builder.Services);

    var app = builder.Build();
    var endpoints = app.NewEndpointRouteBuilder();

    module.MapEndpoints(endpoints);

    var mappedEndpoints = endpoints.DataSources
        .SelectMany(ds => ds.Endpoints)
        .OfType<RouteEndpoint>();

    Assert.That(mappedEndpoints.Any(e => e.RoutePattern.RawText == "/samples"), Is.True);
}
```

## Related ADRs

- **ADR-003:** Validation in MediatR pipeline (used by modules)
- **ADR-004:** Direct DbContext access in services (used within module handlers)
- **ADR-005:** Automatic audit context (used across all modules)

## References

- [Modular Monolith Architecture Patterns](https://www.milanjovanovic.tech/blog/modular-monolith)
- [Feature Modules in .NET](https://github.com/ardalis/CleanArchitecture)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
