using ForgeKit.Api.Handlers;
using ForgeKit.Api.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Handlers;

/// <summary>
/// Tests for ResultQueryHandler base class helper methods and patterns.
/// </summary>
public class ResultQueryHandlerTests
{
    // Sample test request and response
    public record TestQuery : IRequest<Result<string>>;
    public class TestQueryHandler : ResultQueryHandler<TestQuery, string>
    {
        public TestQueryHandler(ILogger<TestQueryHandler> logger) : base(logger)
        {
        }

        public override Task<Result<string>> HandleAsync(TestQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Success("test data"));
        }
    }

    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.Success("data");

        // Assert
        result.ShouldBeOfType<Result<string>.Success>();
        ((Result<string>.Success)result).Data.ShouldBe("data");
    }

    [Fact]
    public void NotFound_CreatesFailureWithResourceType()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.NotFound("Resource not found", "User");

        // Assert
        result.ShouldBeOfType<Result<string>.Failure>();
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("USER_NOT_FOUND");
        failure.Message.ShouldBe("Resource not found");
    }

    [Fact]
    public void NotFound_CreatesFailureWithoutResourceType()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.NotFound("Resource not found");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("RESOURCE_NOT_FOUND");
    }

    [Fact]
    public void Conflict_CreatesFailureWithField()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.Conflict("Already exists", "email");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("EMAIL_CONFLICT");
    }

    [Fact]
    public void Conflict_CreatesFailureWithoutField()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.Conflict("Already exists");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("CONFLICT");
    }

    [Fact]
    public void Unauthorized_CreatesFailureWithOperation()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.Unauthorized("Not allowed", "delete");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("UNAUTHORIZED_DELETE");
    }

    [Fact]
    public void Unauthorized_CreatesFailureWithoutOperation()
    {
        // Act
        var result = ResultQueryHandler<TestQuery, string>.Unauthorized("Not allowed");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public void ValidationError_CreatesFailureWithErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "email", new[] { "Invalid" } }
        };

        // Act
        var result = ResultQueryHandler<TestQuery, string>.ValidationError("Validation failed", errors);

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldBe(errors);
    }

    [Fact]
    public async Task Handler_CanBeExecuted()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryHandler>>();
        var handler = new TestQueryHandler(logger);
        var request = new TestQuery();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<string>.Success>();
        ((Result<string>.Success)result).Data.ShouldBe("test data");
    }
}

/// <summary>
/// Tests for ResultCommandHandler base class helper methods and patterns.
/// </summary>
public class ResultCommandHandlerTests
{
    // Sample test request and response
    public record TestCommand : IRequest<Result<string>>;
    public class TestCommandHandler : ResultCommandHandler<TestCommand, string>
    {
        public TestCommandHandler(ILogger<TestCommandHandler> logger) : base(logger)
        {
        }

        public override Task<Result<string>> HandleAsync(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Success("created"));
        }
    }

    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = ResultCommandHandler<TestCommand, string>.Success("data");

        // Assert
        result.ShouldBeOfType<Result<string>.Success>();
        ((Result<string>.Success)result).Data.ShouldBe("data");
    }

    [Fact]
    public void NotFound_CreatesFailureWithResourceType()
    {
        // Act
        var result = ResultCommandHandler<TestCommand, string>.NotFound("Resource not found", "User");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("USER_NOT_FOUND");
    }

    [Fact]
    public void Conflict_CreatesFailureWithField()
    {
        // Act
        var result = ResultCommandHandler<TestCommand, string>.Conflict("Already exists", "email");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("EMAIL_CONFLICT");
    }

    [Fact]
    public void Unauthorized_CreatesFailureWithOperation()
    {
        // Act
        var result = ResultCommandHandler<TestCommand, string>.Unauthorized("Not allowed", "delete");

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("UNAUTHORIZED_DELETE");
    }

    [Fact]
    public void ValidationError_CreatesFailureWithErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "name", new[] { "Required" } }
        };

        // Act
        var result = ResultCommandHandler<TestCommand, string>.ValidationError("Validation failed", errors);

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe("VALIDATION_ERROR");
        failure.Details.ShouldBe(errors);
    }

    [Fact]
    public async Task Handler_CanBeExecuted()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestCommandHandler>>();
        var handler = new TestCommandHandler(logger);
        var request = new TestCommand();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<string>.Success>();
        ((Result<string>.Success)result).Data.ShouldBe("created");
    }
}
