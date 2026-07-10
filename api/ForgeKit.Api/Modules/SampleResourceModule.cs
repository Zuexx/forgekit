using ForgeKit.Api.Extensions;
using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Models;
using ForgeKit.Api.Results;
using ForgeKit.Api.Samples;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using AspNetResults = Microsoft.AspNetCore.Http.Results;

namespace ForgeKit.Api.Modules;

/// <summary>
/// Sample Resource Module demonstrating Result Pattern with Minimal API.
/// This is a TEMPLATE showing how to structure endpoints using Result-returning handlers.
/// 
/// Applicable to: Any domain entity endpoint (Users, Patients, Orders, etc.)
/// </summary>
public class SampleResourceModule : ISampleModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Handlers are automatically registered by MediatR
        // No additional registration needed
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resources")
            .WithName("Resources")
            .WithTags("Resources");

        // GET /v1/resources/{id} - Get resource by ID
        group.MapGet("{id}", GetResourceById)
            .WithName("GetResourceById")
            .Produces<SampleResourceDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithSummary("Get resource by ID")
            .WithDescription("Retrieves a single resource by its ID. Demonstrates Result Pattern with validation and not-found error handling.");

        // GET /v1/resources - List resources with optional filter
        group.MapGet("/", ListResources)
            .WithName("ListResources")
            .Produces<List<SampleResourceDto>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithSummary("List resources")
            .WithDescription("Lists all resources with optional name filtering. Demonstrates Result Pattern with list operations.");

        // POST /v1/resources - Create new resource
        group.MapPost("/", CreateResource)
            .WithName("CreateResource")
            .Produces<CreatedResourceDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .WithSummary("Create resource")
            .WithDescription("Creates a new resource. Demonstrates Result Pattern with validation, conflict detection, and authorization.");

        return endpoints;
    }

    /// <summary>
    /// GET endpoint handler for retrieving a resource by ID.
    /// Demonstrates Result Pattern with validation and not-found error handling.
    /// </summary>
    private static async Task<IResult> GetResourceById(string id, IMediator mediator)
    {
        var query = new GetResourceByIdQuery(id);
        var result = await mediator.Send(query);

        return result.ToHttpResponse();
    }

    /// <summary>
    /// GET endpoint handler for listing resources with optional filtering.
    /// Demonstrates Result Pattern with list operations and filter validation.
    /// </summary>
    private static async Task<IResult> ListResources(string? filterByName, IMediator mediator)
    {
        var query = new ListResourcesQuery(filterByName);
        var result = await mediator.Send(query);

        return result.ToHttpResponse();
    }

    /// <summary>
    /// POST endpoint handler for creating a new resource.
    /// Demonstrates Result Pattern with validation, conflict detection, and authorization.
    /// </summary>
    private static async Task<IResult> CreateResource(CreateResourceRequest request, IMediator mediator)
    {
        var command = new CreateResourceCommand(request.Name);
        var result = await mediator.Send(command);

        // Return 201 Created with resource data
        return result switch
        {
            Result<CreatedResourceDto>.Success success =>
                AspNetResults.Created($"/v1/resources/{success.Data.Id}", success.Data),

            Result<CreatedResourceDto>.Failure failure =>
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

/// <summary>
/// Request model for creating a resource.
/// </summary>
public record CreateResourceRequest(string Name);
