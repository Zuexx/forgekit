using System.Text.Json;
using ForgeKit.Api.Constants;
using ForgeKit.Api.Exceptions;
using ForgeKit.Api.Middlewares;
using ForgeKit.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ExceptionHandlingMiddleware _middleware;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = new MockLogger<ExceptionHandlingMiddleware>();
        _middleware = new ExceptionHandlingMiddleware(_logger);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_Returns404Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new NotFoundException("Resource not found", "User");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WithConflictException_Returns409Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ConflictException("Resource already exists", "email");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_Returns403Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new UnauthorizedException("Access denied");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_WithDomainException_Returns422Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new TestDomainException("Business rule violated");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_Returns422Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            { "field1", new[] { "Error message 1" } }
        };
        var exception = new ValidationAppException(errors);
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_Returns500Status()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Unexpected error");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_ErrorResponse_IncludesTraceId()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new NotFoundException("Not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.TraceId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithCorrelationIdHeader_UsesHeaderValue()
    {
        // Arrange
        var correlationId = "test-correlation-id-123";
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = correlationId;
        var exception = new NotFoundException("Not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.TraceId.ShouldBe(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationIdHeader_UsesTraceIdentifier()
    {
        // Arrange
        var context = CreateHttpContext();
        var expectedTraceId = context.TraceIdentifier;
        var exception = new NotFoundException("Not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.TraceId.ShouldBe(expectedTraceId);
    }

    [Fact]
    public async Task InvokeAsync_CorrelationIdStoredInHttpContext()
    {
        // Arrange
        var correlationId = "trace-123";
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = correlationId;
        var exception = new NotFoundException("Not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Items["X-Correlation-ID"].ShouldBe(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_ErrorResponse_CorrectFormat()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new NotFoundException("Test resource not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.ShouldNotBeNull();
        response.Title.ShouldBe("Not Found");
        response.Status.ShouldBe(StatusCodes.Status404NotFound);
        response.Detail.ShouldBe("Test resource not found");
        response.TraceId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_IncludesErrors()
    {
        // Arrange
        var context = CreateHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            { "email", new[] { "Invalid email format" } },
            { "age", new[] { "Must be at least 18" } }
        };
        var exception = new ValidationAppException(errors);
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.Errors.ShouldNotBeNull();
        response.Errors.Count.ShouldBe(2);
        response.Errors["email"].ShouldContain("Invalid email format");
        response.Errors["age"].ShouldContain("Must be at least 18");
    }

    [Fact]
    public async Task InvokeAsync_DomainException_IncludesDetails()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new TestDomainException("Business rule violated");
        exception.Details = new Dictionary<string, string[]>
        {
            { "field1", new[] { "Error detail 1" } }
        };
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        response.Errors.ShouldNotBeNull();
        response.Errors["field1"].ShouldContain("Error detail 1");
    }

    [Fact]
    public async Task InvokeAsync_BackwardCompatibility_BusinessLogicException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new BusinessLogicException("Business logic error");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    public static IEnumerable<object[]> ErrorMappings()
    {
        yield return new object[]
        {
            new ValidationAppException(new Dictionary<string, string[]>
            {
                { "name", new[] { "Name is required" } }
            }),
            StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.ValidationError,
            "Validation Error"
        };
        yield return new object[]
        {
            new NotFoundException("Resource not found"),
            StatusCodes.Status404NotFound,
            ErrorCodes.ResourceNotFound,
            "Not Found"
        };
        yield return new object[]
        {
            new ConflictException("Resource already exists"),
            StatusCodes.Status409Conflict,
            ErrorCodes.ConflictError,
            "Conflict"
        };
        yield return new object[]
        {
            new UnauthorizedException("Access denied"),
            StatusCodes.Status403Forbidden,
            ErrorCodes.UnauthorizedError,
            "Forbidden"
        };
        yield return new object[]
        {
            new BusinessLogicException("Business rule violated"),
            StatusCodes.Status400BadRequest,
            ErrorCodes.BusinessLogicError,
            "Business Rule Violation"
        };
        yield return new object[]
        {
            new InvalidStateException("Resource is not in a valid state"),
            StatusCodes.Status400BadRequest,
            ErrorCodes.InvalidStateError,
            "Invalid State"
        };
        yield return new object[]
        {
            new InvalidOperationException("Unexpected error"),
            StatusCodes.Status500InternalServerError,
            ErrorCodes.InternalServerError,
            "Server Error"
        };
    }

    [Theory]
    [MemberData(nameof(ErrorMappings))]
    public async Task InvokeAsync_ErrorResponse_MapsStatusCodeAndErrorCode(
        Exception exception,
        int expectedStatusCode,
        string expectedErrorCode,
        string expectedTitle)
    {
        // Arrange
        var context = CreateHttpContext();
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        var response = GetErrorResponse(context);
        context.Response.StatusCode.ShouldBe(expectedStatusCode);
        response.Status.ShouldBe(expectedStatusCode);
        response.Code.ShouldBe(expectedErrorCode);
        response.Title.ShouldBe(expectedTitle);
        response.Message.ShouldBe(exception.Message);
        response.Detail.ShouldBe(exception.Message);
        response.Timestamp.ShouldNotBe(default);
        response.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_ResponseContentType_IsJson()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new NotFoundException("Not found");
        var next = CreateNextDelegate(() => throw exception);

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        context.Response.ContentType.ShouldBe("application/json");
    }

    // Helper methods
    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static RequestDelegate CreateNextDelegate(Action throwAction)
    {
        return (context) =>
        {
            throwAction();
            return Task.CompletedTask;
        };
    }

    private static ErrorResponse GetErrorResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var json = reader.ReadToEnd();
        var response = JsonSerializer.Deserialize<ErrorResponse>(json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return response!;
    }

    private class TestDomainException : DomainException
    {
        public TestDomainException(string message) : base(message, "TEST_ERROR")
        {
        }
    }
}

internal class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
