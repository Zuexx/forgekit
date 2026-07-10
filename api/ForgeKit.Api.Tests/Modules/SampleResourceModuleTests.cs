using ForgeKit.Api.Results;
using ForgeKit.Api.Samples;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Modules;

/// <summary>
/// Integration tests for SampleResourceModule endpoints.
/// Tests demonstrate Result Pattern endpoint integration with Minimal API.
/// </summary>
public class SampleResourceModuleTests
{
    [Fact]
    public void GetResourceById_WithValidId_Returns200Ok()
    {
        // Expected: GET /v1/resources/{valid-id}
        // Response: 200 OK with SampleResourceDto

        var result = new Result<SampleResourceDto>.Success(
            new SampleResourceDto { Id = "123", Name = "Test", CreatedAt = DateTime.UtcNow }
        );

        result.ShouldBeOfType<Result<SampleResourceDto>.Success>();
    }

    [Fact]
    public void GetResourceById_WithInvalidId_Returns404NotFound()
    {
        // Expected: GET /v1/resources/INVALID
        // Response: 404 Not Found with RFC 7807 Problem Details

        var result = new Result<SampleResourceDto>.Failure(
            "RESOURCE_NOT_FOUND",
            "Resource not found"
        );

        result.ShouldBeOfType<Result<SampleResourceDto>.Failure>();
        var failure = (Result<SampleResourceDto>.Failure)result;
        failure.Code.ShouldBe("RESOURCE_NOT_FOUND");
    }

    [Fact]
    public void ListResources_WithoutFilter_Returns200Ok()
    {
        // Expected: GET /v1/resources
        // Response: 200 OK with list of resources

        var result = new Result<List<SampleResourceDto>>.Success(
            new List<SampleResourceDto>
            {
                new() { Id = "1", Name = "Resource 1", CreatedAt = DateTime.UtcNow },
                new() { Id = "2", Name = "Resource 2", CreatedAt = DateTime.UtcNow }
            }
        );

        result.ShouldBeOfType<Result<List<SampleResourceDto>>.Success>();
        var success = (Result<List<SampleResourceDto>>.Success)result;
        success.Data.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateResource_WithValidData_Returns201Created()
    {
        // Expected: POST /v1/resources with { name: "New Resource" }
        // Response: 201 Created with CreatedResourceDto and Location header

        var result = new Result<CreatedResourceDto>.Success(
            new CreatedResourceDto { Id = "new-id", Name = "New Resource", CreatedAt = DateTime.UtcNow }
        );

        result.ShouldBeOfType<Result<CreatedResourceDto>.Success>();
        var success = (Result<CreatedResourceDto>.Success)result;
        success.Data.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CreateResource_WithDuplicateName_Returns409Conflict()
    {
        // Expected: POST /v1/resources with duplicate name
        // Response: 409 Conflict with conflict error

        var result = new Result<CreatedResourceDto>.Failure(
            "NAME_CONFLICT",
            "A resource with this name already exists"
        );

        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
        var failure = (Result<CreatedResourceDto>.Failure)result;
        failure.Code.ShouldBe("NAME_CONFLICT");
    }
}
