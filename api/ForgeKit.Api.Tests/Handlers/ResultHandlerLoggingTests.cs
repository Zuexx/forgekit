using ForgeKit.Api.Samples;
using ForgeKit.Api.Results;
using Shouldly;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ForgeKit.Api.Tests.Handlers;

/// <summary>
/// Tests for structured logging in command and query handlers
/// Verifies that handlers execute successfully and produce logs
/// </summary>
public class ResultCommandHandlerLoggingTests
{
    [Fact]
    public async Task CreateResourceCommandHandler_ExecutesSuccessfully()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(mockLogger);
        var command = new CreateResourceCommand("Test Resource");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<CreatedResourceDto>.Success>();
    }

    [Fact]
    public async Task CreateResourceCommandHandler_LogsOnSuccess()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(mockLogger);
        var command = new CreateResourceCommand("New Test Resource");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<CreatedResourceDto>.Success>();

        // Verify at least some logging occurred
        mockLogger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception?, string>>());
    }

    [Fact]
    public async Task CreateResourceCommandHandler_HandlesFailure()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<CreateResourceCommandHandler>>();
        var handler = new CreateResourceCommandHandler(mockLogger);
        var command = new CreateResourceCommand("Existing"); // This triggers conflict in the sample

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<CreatedResourceDto>.Failure>();
    }
}

/// <summary>
/// Tests for query handler logging
/// </summary>
public class ResultQueryHandlerLoggingTests
{
    [Fact]
    public async Task GetResourceByIdQueryHandler_ExecutesSuccessfully()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(mockLogger);
        var query = new GetResourceByIdQuery("test-id");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetResourceByIdQueryHandler_ReturnsSuccessForValidId()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(mockLogger);
        var query = new GetResourceByIdQuery("valid-id");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<SampleResourceDto>.Success>();
    }

    [Fact]
    public async Task GetResourceByIdQueryHandler_ReturnsFailureForInvalidId()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<GetResourceByIdQueryHandler>>();
        var handler = new GetResourceByIdQueryHandler(mockLogger);
        var query = new GetResourceByIdQuery("INVALID"); // This triggers not found

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<SampleResourceDto>.Failure>();
    }

    [Fact]
    public async Task ListResourcesQueryHandler_ReturnsResults()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<ListResourcesQueryHandler>>();
        var handler = new ListResourcesQueryHandler(mockLogger);
        var query = new ListResourcesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Result<List<SampleResourceDto>>.Success>();

        var successResult = result as Result<List<SampleResourceDto>>.Success;
        successResult!.Data.Count.ShouldBeGreaterThan(0);
    }
}
