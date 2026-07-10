using ForgeKit.Api.Handlers;
using ForgeKit.Api.Results;
using MediatR;

namespace ForgeKit.Api.Samples;

/// <summary>
/// Generic sample query request for listing resources with optional filtering.
/// This is a TEMPLATE showing how to structure list/query operations.
/// 
/// Applicable to: Any domain entity listing (Users, Patients, Orders, etc.)
/// </summary>
public record ListResourcesQuery(string? FilterByName = null) : IRequest<Result<List<SampleResourceDto>>>;

/// <summary>
/// Generic sample query handler demonstrating Result Pattern for list operations.
/// This is a TEMPLATE showing filter validation and successful list returns.
/// 
/// Applicable to: Any read operation returning collections.
/// </summary>
public class ListResourcesQueryHandler(ILogger<ListResourcesQueryHandler> logger) : ResultQueryHandler<ListResourcesQuery, List<SampleResourceDto>>(logger)
{
    // In a real implementation, this would be injected dependencies:
    // private readonly IRepository<SampleResource> _repository;
    // private readonly IMapper _mapper;

    public override async Task<Result<List<SampleResourceDto>>> HandleAsync(
        ListResourcesQuery request,
        CancellationToken cancellationToken)
    {
        // Optional: Validate filter parameters
        if (!string.IsNullOrWhiteSpace(request.FilterByName) && request.FilterByName.Length < 2)
        {
            var validationErrors = new Dictionary<string, string[]>
            {
                { "filterByName", new[] { "Filter name must be at least 2 characters" } }
            };
            return ValidationError("Invalid filter parameters", validationErrors);
        }

        // Simulate repository query with optional filtering
        // In real code:
        // var query = _repository.AsQueryable();
        // if (!string.IsNullOrWhiteSpace(request.FilterByName))
        //     query = query.Where(r => r.Name.Contains(request.FilterByName));
        // var resources = await query.ToListAsync(cancellationToken);

        var resources = SimulateResourceLookupWithFilter(request.FilterByName);

        // Map to DTOs
        // In real code: var dtos = _mapper.Map<List<SampleResourceDto>>(resources);
        var dtos = resources.Select(r => new SampleResourceDto
        {
            Id = r.Id,
            Name = r.Name,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Success(dtos);
    }

    // Simulation helper - replace with actual repository
    private List<SampleResource> SimulateResourceLookupWithFilter(string? filterByName)
    {
        // Simulate database resources
        var allResources = new List<SampleResource>
        {
            new() { Id = "1", Name = "Alpha Resource", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = "2", Name = "Beta Resource", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = "3", Name = "Gamma Resource", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        if (string.IsNullOrWhiteSpace(filterByName))
            return allResources;

        return allResources
            .Where(r => r.Name.Contains(filterByName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
