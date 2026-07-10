using ForgeKit.Api.Handlers;
using ForgeKit.Api.Results;
using MediatR;

namespace ForgeKit.Api.Samples;

/// <summary>
/// Generic sample query request for retrieving a resource by ID.
/// This is a TEMPLATE showing how to structure Result Pattern queries.
/// 
/// Applicable to: Any domain entity lookup (User, Patient, Order, etc.)
/// </summary>
public record GetResourceByIdQuery(string Id) : IRequest<Result<SampleResourceDto>>;

/// <summary>
/// Generic sample query handler demonstrating Result Pattern usage.
/// This is a TEMPLATE showing proper error handling and Result usage.
/// 
/// Applicable to: Any read operation that may fail with business errors.
/// </summary>
public class GetResourceByIdQueryHandler(ILogger<GetResourceByIdQueryHandler> logger) : ResultQueryHandler<GetResourceByIdQuery, SampleResourceDto>(logger)
{
    // In a real implementation, this would be injected dependencies:
    // private readonly IRepository<SampleResource> _repository;
    // private readonly IMapper _mapper;

    public override async Task<Result<SampleResourceDto>> HandleAsync(
        GetResourceByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Validate input - example of validation error
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            var validationErrors = new Dictionary<string, string[]>
            {
                { "id", new[] { "ID cannot be empty" } }
            };
            return ValidationError("Input validation failed", validationErrors);
        }

        // Simulate repository lookup
        // In real code: var resource = await _repository.GetByIdAsync(request.Id, cancellationToken);
        var resource = SimulateRepositoryLookup(request.Id);

        // Check if resource exists - example of not found error
        if (resource == null)
        {
            return NotFound($"Resource with ID '{request.Id}' not found", "Resource");
        }

        // Map to DTO and return success
        // In real code: var dto = _mapper.Map<SampleResourceDto>(resource);
        var dto = new SampleResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            CreatedAt = resource.CreatedAt
        };

        return Success(dto);
    }

    // Simulation helper - replace with actual repository
    private SampleResource? SimulateRepositoryLookup(string id)
    {
        // In a real implementation, query the database
        return id == "INVALID" ? null : new SampleResource
        {
            Id = id,
            Name = "Sample Resource",
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Generic sample DTO - represents serializable resource data.
/// </summary>
public class SampleResourceDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Generic sample domain entity - represents internal resource.
/// </summary>
internal class SampleResource
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
