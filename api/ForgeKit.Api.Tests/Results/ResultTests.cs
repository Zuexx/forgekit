using ForgeKit.Api.Results;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Results;

/// <summary>
/// Comprehensive unit tests for Result<T> type and extension methods.
/// Tests cover all success/failure scenarios and composition operations.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_WithData_CreatesSuccessResult()
    {
        // Arrange
        var data = "test data";

        // Act
        var result = new Result<string>.Success(data);

        // Assert
        result.ShouldBeOfType<Result<string>.Success>();
        ((Result<string>.Success)result).Data.ShouldBe(data);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_CreatesFailureResult()
    {
        // Arrange
        var code = "TEST_ERROR";
        var message = "Test error message";

        // Act
        var result = new Result<string>.Failure(code, message);

        // Assert
        result.ShouldBeOfType<Result<string>.Failure>();
        var failure = (Result<string>.Failure)result;
        failure.Code.ShouldBe(code);
        failure.Message.ShouldBe(message);
        failure.Details.ShouldBeNull();
    }

    [Fact]
    public void Failure_WithDetails_IncludesFieldErrors()
    {
        // Arrange
        var code = "VALIDATION_ERROR";
        var message = "Validation failed";
        var details = new Dictionary<string, string[]>
        {
            { "email", new[] { "Invalid email format" } },
            { "age", new[] { "Must be 18 or older" } }
        };

        // Act
        var result = new Result<string>.Failure(code, message, details);

        // Assert
        var failure = (Result<string>.Failure)result;
        failure.Details.ShouldBe(details);
    }

    [Fact]
    public void Map_WithSuccessResult_TransformsData()
    {
        // Arrange
        var result = new Result<int>.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.ShouldBeOfType<Result<int>.Success>();
        ((Result<int>.Success)mapped).Data.ShouldBe(10);
    }

    [Fact]
    public void Map_WithFailureResult_ReturnsFailure()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR", "Something failed");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.ShouldBeOfType<Result<int>.Failure>();
        var failure = (Result<int>.Failure)mapped;
        failure.Code.ShouldBe("ERROR");
    }

    [Fact]
    public void Bind_WithSuccessResult_ChainsOperations()
    {
        // Arrange
        var result = new Result<int>.Success(5);

        // Act
        var bound = result.Bind(x => new Result<int>.Success(x * 2));

        // Assert
        bound.ShouldBeOfType<Result<int>.Success>();
        ((Result<int>.Success)bound).Data.ShouldBe(10);
    }

    [Fact]
    public void Bind_WithFailureResult_ReturnsFailure()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR", "Failed");

        // Act
        var bound = result.Bind(x => new Result<int>.Success(x * 2));

        // Assert
        bound.ShouldBeOfType<Result<int>.Failure>();
    }

    [Fact]
    public void Bind_WithNestedFailure_PreservesFirstFailure()
    {
        // Arrange
        var result = new Result<int>.Success(5);

        // Act
        var bound = result.Bind(x => new Result<int>.Failure("NESTED_ERROR", "Nested failed"));

        // Assert
        bound.ShouldBeOfType<Result<int>.Failure>();
        var failure = (Result<int>.Failure)bound;
        failure.Code.ShouldBe("NESTED_ERROR");
    }

    [Fact]
    public async Task BindAsync_WithSuccessResult_ChainsAsyncOperations()
    {
        // Arrange
        var result = new Result<int>.Success(5);

        // Act
        var bound = await result.BindAsync<int, int>(async x =>
        {
            await Task.Delay(10);
            return new Result<int>.Success(x * 2);
        });

        // Assert
        bound.ShouldBeOfType<Result<int>.Success>();
        ((Result<int>.Success)bound).Data.ShouldBe(10);
    }

    [Fact]
    public async Task BindAsync_WithFailureResult_ReturnsFailure()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR", "Failed");

        // Act
        var bound = await result.BindAsync<int, int>(async x =>
        {
            await Task.Delay(10);
            return new Result<int>.Success(x * 2);
        });

        // Assert
        bound.ShouldBeOfType<Result<int>.Failure>();
    }

    [Fact]
    public void Match_WithSuccess_CallsSuccessCallback()
    {
        // Arrange
        var result = new Result<int>.Success(42);
        var successCalled = false;
        var failureCalled = false;

        // Act
        var output = result.Match(
            onSuccess: x => { successCalled = true; return x * 2; },
            onFailure: (_, _, _) => { failureCalled = true; return 0; }
        );

        // Assert
        successCalled.ShouldBeTrue();
        failureCalled.ShouldBeFalse();
        output.ShouldBe(84);
    }

    [Fact]
    public void Match_WithFailure_CallsFailureCallback()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR", "Test error");
        var successCalled = false;
        var failureCalled = false;

        // Act
        var output = result.Match(
            onSuccess: x => { successCalled = true; return x * 2; },
            onFailure: (code, message, _) =>
            {
                failureCalled = true;
                return code == "ERROR" && message == "Test error" ? -1 : -999;
            }
        );

        // Assert
        successCalled.ShouldBeFalse();
        failureCalled.ShouldBeTrue();
        output.ShouldBe(-1);
    }

    [Fact]
    public void OnSuccess_WithSuccess_ExecutesAction()
    {
        // Arrange
        var result = new Result<int>.Success(5);
        var actionExecuted = false;
        var actionValue = 0;

        // Act
        var returned = result.OnSuccess(x => { actionExecuted = true; actionValue = x; });

        // Assert
        actionExecuted.ShouldBeTrue();
        actionValue.ShouldBe(5);
        returned.ShouldBe(result);
    }

    [Fact]
    public void OnSuccess_WithFailure_DoesNotExecuteAction()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR", "Failed");
        var actionExecuted = false;

        // Act
        var returned = result.OnSuccess(x => { actionExecuted = true; });

        // Assert
        actionExecuted.ShouldBeFalse();
        returned.ShouldBe(result);
    }

    [Fact]
    public void OnFailure_WithFailure_ExecutesAction()
    {
        // Arrange
        var result = new Result<int>.Failure("ERROR_CODE", "Error message");
        var actionExecuted = false;
        var capturedCode = "";
        var capturedMessage = "";

        // Act
        var returned = result.OnFailure((code, message) =>
        {
            actionExecuted = true;
            capturedCode = code;
            capturedMessage = message;
        });

        // Assert
        actionExecuted.ShouldBeTrue();
        capturedCode.ShouldBe("ERROR_CODE");
        capturedMessage.ShouldBe("Error message");
        returned.ShouldBe(result);
    }

    [Fact]
    public void OnFailure_WithSuccess_DoesNotExecuteAction()
    {
        // Arrange
        var result = new Result<int>.Success(5);
        var actionExecuted = false;

        // Act
        var returned = result.OnFailure((_, _) => { actionExecuted = true; });

        // Assert
        actionExecuted.ShouldBeFalse();
        returned.ShouldBe(result);
    }

    [Fact]
    public void GetValueOrThrow_WithSuccess_ReturnsData()
    {
        // Arrange
        var result = new Result<string>.Success("test data");

        // Act
        var value = result.GetValueOrThrow();

        // Assert
        value.ShouldBe("test data");
    }

    [Fact]
    public void GetValueOrThrow_WithFailure_ThrowsException()
    {
        // Arrange
        var result = new Result<string>.Failure("ERROR", "Failed");

        // Act
        var action = () => result.GetValueOrThrow();

        // Assert
        Should.Throw<InvalidOperationException>(action)
            .Message.ShouldContain("Result failed with code 'ERROR': Failed");
    }

    [Fact]
    public void GetValueOrThrow_WithCustomExceptionFactory_ThrowsCustomException()
    {
        // Arrange
        var result = new Result<string>.Failure("NOT_FOUND", "Resource not found");

        // Act
        var action = () => result.GetValueOrThrow((code, message, _) =>
            new KeyNotFoundException($"[{code}] {message}")
        );

        // Assert
        Should.Throw<KeyNotFoundException>(action)
            .Message.ShouldContain("[NOT_FOUND] Resource not found");
    }

    [Fact]
    public void ChainedOperations_WorkCorrectly()
    {
        // Arrange
        var result = new Result<int>.Success(5);

        // Act
        var final = result
            .Map(x => x * 2)          // 10
            .Map(x => x + 3)          // 13
            .Bind<int, int>(x => x > 10 
                ? new Result<int>.Success(x * 2)    // 26
                : new Result<int>.Failure("TOO_SMALL", "Value too small"));

        // Assert
        final.ShouldBeOfType<Result<int>.Success>();
        ((Result<int>.Success)final).Data.ShouldBe(26);
    }

    [Fact]
    public void ChainedOperations_WithFailure_StopsChain()
    {
        // Arrange
        var result = new Result<int>.Success(2);

        // Act
        var final = result
            .Map(x => x * 2)          // 4
            .Bind<int, int>(x => x > 10 
                ? new Result<int>.Success(x * 2)
                : new Result<int>.Failure("TOO_SMALL", "Value too small"))
            .Map(x => x * 100);       // Should not execute

        // Assert
        final.ShouldBeOfType<Result<int>.Failure>();
        var failure = (Result<int>.Failure)final;
        failure.Code.ShouldBe("TOO_SMALL");
    }
}
