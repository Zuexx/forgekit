# XML Documentation Guide

This guide defines how API XML comments should be written so IDE IntelliSense, generated `Api.xml`, OpenAPI, and Scalar remain useful after the project is forked.

## Goals

- Document public contracts, not implementation noise.
- Keep examples domain-neutral so forks can copy them safely.
- Make endpoint, handler, DTO, and error behavior understandable without reading implementation code.

## Comment Conventions

### Types

Use a short noun phrase for types. Add `remarks` only when lifecycle, behavior, or constraints matter.

```csharp
/// <summary>
/// Represents a request to create a resource.
/// </summary>
/// <remarks>
/// The command is validated by FluentValidation before reaching the handler.
/// Business rules are enforced by the handler or domain service.
/// </remarks>
public sealed record CreateResourceCommand : IRequest<Result<ResourceDto>>;
```

### Methods

Use an active verb phrase. Document parameters, cancellation tokens, return shape, and expected exceptions.

```csharp
/// <summary>
/// Creates a resource and returns the persisted representation.
/// </summary>
/// <param name="request">The validated creation request.</param>
/// <param name="cancellationToken">Token used to cancel the operation.</param>
/// <returns>The created resource when the operation succeeds.</returns>
/// <exception cref="ConflictException">
/// Thrown when a resource with the same natural key already exists.
/// Returns HTTP 409 with error code <c>CONFLICT</c>.
/// </exception>
public Task<Result<ResourceDto>> Handle(
    CreateResourceCommand request,
    CancellationToken cancellationToken);
```

### Properties

Describe meaning and format. Avoid restating the property name.

```csharp
/// <summary>
/// Stable identifier of the workspace that owns the resource.
/// </summary>
public required string WorkspaceId { get; init; }
```

### Exceptions

Document expected API-facing exceptions with the condition, status code, and error code.

```csharp
/// <exception cref="NotFoundException">
/// Thrown when the requested resource does not exist.
/// Returns HTTP 404 with error code <c>NOT_FOUND</c>.
/// </exception>
```

## Templates

### Command Handler

```csharp
/// <summary>
/// Handles creation of a resource.
/// </summary>
/// <remarks>
/// The handler assumes syntactic validation has already run. It checks business
/// rules, persists changes through the unit of work, and returns a Result value
/// for expected success or failure outcomes.
/// </remarks>
public sealed class CreateResourceCommandHandler
    : IRequestHandler<CreateResourceCommand, Result<ResourceDto>>
{
    /// <summary>
    /// Creates the resource.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A result containing the created resource.</returns>
    public Task<Result<ResourceDto>> Handle(
        CreateResourceCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

### Query Handler

```csharp
/// <summary>
/// Handles lookup of a resource by identifier.
/// </summary>
public sealed class GetResourceByIdQueryHandler
    : IRequestHandler<GetResourceByIdQuery, Result<ResourceDto>>
{
    /// <summary>
    /// Loads the resource matching the requested identifier.
    /// </summary>
    /// <param name="request">The lookup query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A result containing the resource or a not-found failure.</returns>
    public Task<Result<ResourceDto>> Handle(
        GetResourceByIdQuery request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

### Endpoint Module

```csharp
/// <summary>
/// Registers resource endpoints.
/// </summary>
public sealed class ResourceModule : IModule
{
    /// <summary>
    /// Maps resource HTTP routes.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/resources", CreateResourceAsync)
            .WithSummary("Create resource")
            .WithDescription("Creates a resource in the current workspace.")
            .Produces<ResourceDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);
    }
}
```

### DTO

```csharp
/// <summary>
/// Resource returned by the API.
/// </summary>
public sealed record ResourceDto
{
    /// <summary>
    /// Stable identifier assigned by the API.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string Name { get; init; }
}
```

## Review Checklist

Before approving API changes, check:

- Public handlers, commands, queries, DTOs, modules, middleware, and interfaces have XML comments.
- Endpoint routes include `WithSummary`, `WithDescription`, and `Produces` metadata.
- DTO property comments describe meaning, format, and important constraints.
- Expected API-facing exceptions include status code and error code.
- Comments use generic starter-kit language unless the feature is intentionally domain-specific.
- XML comments explain public behavior without duplicating implementation line by line.
- `dotnet build` generates `Api.xml` without documentation-related errors.
- Scalar/OpenAPI output exposes useful summaries and response metadata.
