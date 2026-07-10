using ForgeKit.Api.Results;
using ForgeKit.Api.Samples;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Samples;

/// <summary>
/// Tests for sample query handlers demonstrating Result Pattern usage.
/// These tests show how to test Result-returning handlers universally.
/// </summary>
public class SampleQueryHandlerTests
{
    [Fact]
    public async Task GetResourceByIdQueryHandler_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(logger);
        var query = new GetResourceByIdQuery("valid-id");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<SampleResourceDto>.Success>();
        var success = (Result<SampleResourceDto>.Success)result;
        success.Data.Id.ShouldBe("valid-id");
        success.Data.Name.ShouldBe("Sample Resource");
    }

    [Fact]
    public async Task GetResourceByIdQueryHandler_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var logger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(logger);
        var query = new GetResourceByIdQuery("INVALID");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<SampleResourceDto>.Failure>();
        var failure = (Result<SampleResourceDto>.Failure)result;
        failure.Code.ShouldBe("RESOURCE_NOT_FOUND");
        failure.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task GetResourceByIdQueryHandler_WithEmptyId_ReturnsValidationError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(logger);
        var query = new GetResourceByIdQuery("");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<SampleResourceDto>.Failure>();
        var failure = (Result<SampleResourceDto>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldNotBeNull();
        failure.Details.ShouldContainKey("id");
    }

    [Fact]
    public async Task ListResourcesQueryHandler_WithoutFilter_ReturnsAllResources()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ListResourcesQueryHandler>>();
        var handler = new ListResourcesQueryHandler(logger);
        var query = new ListResourcesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<List<SampleResourceDto>>.Success>();
        var success = (Result<List<SampleResourceDto>>.Success)result;
        success.Data.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ListResourcesQueryHandler_WithFilter_ReturnsFilteredResources()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ListResourcesQueryHandler>>();
        var handler = new ListResourcesQueryHandler(logger);
        var query = new ListResourcesQuery("Beta");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<List<SampleResourceDto>>.Success>();
        var success = (Result<List<SampleResourceDto>>.Success)result;
        success.Data.Count.ShouldBe(1);
        success.Data[0].Name.ShouldContain("Beta");
    }

    [Fact]
    public async Task ListResourcesQueryHandler_WithShortFilter_ReturnsValidationError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ListResourcesQueryHandler>>();
        var handler = new ListResourcesQueryHandler(logger);
        var query = new ListResourcesQuery("A");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<List<SampleResourceDto>>.Failure>();
        var failure = (Result<List<SampleResourceDto>>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
    }
}

/// <summary>
/// Tests for sample command handlers demonstrating Result Pattern with business rules.
/// These tests show how to test command handlers with validation and conflict detection.
/// </summary>
public class SampleCommandHandlerTests
{
    [Fact]
    public async Task CreateResourceCommandHandler_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(logger);
        var command = new CreateResourceCommand("New Resource");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<CreatedResourceDto>.Success>();
        var success = (Result<CreatedResourceDto>.Success)result;
        success.Data.Name.ShouldBe("New Resource");
        success.Data.Id.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CreateResourceCommandHandler_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(logger);
        var command = new CreateResourceCommand("");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
        var failure = (Result<CreatedResourceDto>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldNotBeNull();
        failure.Details.ShouldContainKey("name");
    }

    [Fact]
    public async Task CreateResourceCommandHandler_WithShortName_ReturnsValidationError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(logger);
        var command = new CreateResourceCommand("AB");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
        var failure = (Result<CreatedResourceDto>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldNotBeNull();
        failure.Details.ShouldContainKey("name");
        failure.Details["name"].ShouldContain(s => s.Contains("at least 3 characters long"));
    }

    [Fact]
    public async Task CreateResourceCommandHandler_WithExistingName_ReturnsConflict()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(logger);
        var command = new CreateResourceCommand("Existing");  // "existing" triggers conflict in simulation

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
        var failure = (Result<CreatedResourceDto>.Failure)result;
        failure.Code.ShouldBe("NAME_CONFLICT");
        failure.Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task CreateResourceCommandHandler_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange - first check empty name
        var logger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(logger);
        var command = new CreateResourceCommand("");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
        var failure = (Result<CreatedResourceDto>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldNotBeEmpty();
    }
}
