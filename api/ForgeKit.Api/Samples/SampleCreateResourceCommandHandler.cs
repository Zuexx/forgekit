using ForgeKit.Api.Handlers;
using ForgeKit.Api.Results;
using MediatR;

namespace ForgeKit.Api.Samples;

/// <summary>
/// Generic sample command request for creating a new resource.
/// This is a TEMPLATE showing how to structure Result Pattern commands.
/// 
/// Applicable to: Any domain entity creation (User, Patient, Order, etc.)
/// </summary>
public record CreateResourceCommand(string Name) : IRequest<Result<CreatedResourceDto>>;

/// <summary>
/// Generic sample command handler demonstrating Result Pattern usage with business rule validation.
/// This is a TEMPLATE showing error handling for conflicts and validation.
/// 
/// Applicable to: Any write operation that may fail with business errors.
/// </summary>
public class CreateResourceCommandHandler(ILogger<CreateResourceCommandHandler> logger) : ResultCommandHandler<CreateResourceCommand, CreatedResourceDto>(logger)
{
    // In a real implementation, this would be injected dependencies:
    // private readonly IRepository<SampleResource> _repository;
    // private readonly IMapper _mapper;
    // private readonly IUnitOfWork _unitOfWork;

    public override async Task<Result<CreatedResourceDto>> HandleAsync(
        CreateResourceCommand request,
        CancellationToken cancellationToken)
    {
        // Input validation - example of validation error
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            var validationErrors = new Dictionary<string, string[]>
            {
                { "name", new[] { "Name cannot be empty" } }
            };
            return ValidationError("Input validation failed", validationErrors);
        }

        if (request.Name.Length < 3)
        {
            var validationErrors = new Dictionary<string, string[]>
            {
                { "name", new[] { "Name must be at least 3 characters long" } }
            };
            return ValidationError("Input validation failed", validationErrors);
        }

        // Business rule validation - check for conflicts
        // In real code: var existing = await _repository.FindByNameAsync(request.Name, cancellationToken);
        var existing = SimulateExistingResourceLookup(request.Name);

        if (existing != null)
        {
            return Conflict(
                $"A resource with name '{request.Name}' already exists",
                "name"
            );
        }

        // Business rule validation - example of unauthorized
        // In real code: var isAllowed = await _authorizationService.CanCreate(user, cancellationToken);
        var isAllowed = SimulateAuthorizationCheck();

        if (!isAllowed)
        {
            return Unauthorized("You do not have permission to create resources", "create");
        }

        // Create new resource
        var newResource = new SampleResource
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        // Simulate persistence
        // In real code:
        // _repository.Add(newResource);
        // await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new CreatedResourceDto
        {
            Id = newResource.Id,
            Name = newResource.Name,
            CreatedAt = newResource.CreatedAt
        };

        return Success(dto);
    }

    // Simulation helpers - replace with actual repository/auth checks
    private SampleResource? SimulateExistingResourceLookup(string name)
    {
        // In a real implementation, query the database
        return name.ToLower() == "existing" ? new SampleResource { Id = "1", Name = name, CreatedAt = DateTime.UtcNow } : null;
    }

    private bool SimulateAuthorizationCheck()
    {
        // In a real implementation, check authorization
        return true;
    }
}

/// <summary>
/// Generic sample DTO for created resource response.
/// </summary>
public class CreatedResourceDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
