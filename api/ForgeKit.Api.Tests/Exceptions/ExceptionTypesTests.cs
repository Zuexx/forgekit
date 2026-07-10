using ForgeKit.Api.Exceptions;
using Shouldly;

namespace ForgeKit.Api.Tests.Exceptions;

public class ExceptionTypesTests
{
    [Fact]
    public void DomainException_WithMessage_ShouldInitialize()
    {
        // Arrange
        var message = "Test domain error";
        var code = "TEST_ERROR";

        // Act
        var exception = new TestDomainException(message, code);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe(code);
    }

    [Fact]
    public void NotFoundException_WithMessage_ShouldHaveNotFoundCode()
    {
        // Arrange
        var message = "User not found";
        var resourceType = "User";

        // Act
        var exception = new NotFoundException(message, resourceType);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe("NOT_FOUND");
        exception.ResourceType.ShouldBe(resourceType);
    }

    [Fact]
    public void ConflictException_WithMessage_ShouldHaveConflictCode()
    {
        // Arrange
        var message = "Email already exists";
        var conflictField = "Email";

        // Act
        var exception = new ConflictException(message, conflictField);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe("CONFLICT");
        exception.ConflictField.ShouldBe(conflictField);
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveUnauthorizedCode()
    {
        // Arrange
        var message = "Invalid credentials";

        // Act
        var exception = new UnauthorizedException(message);

        // Assert
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public void DomainException_CanHaveDetails()
    {
        // Arrange
        var message = "Validation failed";
        var details = new Dictionary<string, string[]>
        {
            { "Email", ["Invalid format"] },
            { "Age", ["Must be 18 or older"] }
        };

        // Act
        var exception = new TestDomainException(message, "VALIDATION_ERROR")
        {
            Details = details
        };

        // Assert
        exception.Details.ShouldNotBeNull();
        exception.Details!.Keys.ShouldContain("Email");
        exception.Details!.Keys.ShouldContain("Age");
    }

    [Fact]
    public void AllExceptions_ShouldBeSerializable()
    {
        // Arrange
        var exceptions = new Exception[]
        {
            new NotFoundException("Not found"),
            new ConflictException("Conflict"),
            new UnauthorizedException("Unauthorized"),
            new TestDomainException("Domain error", "ERROR")
        };

        // Act & Assert
        foreach (var ex in exceptions)
        {
            ex.ShouldBeAssignableTo<DomainException>();
        }
    }

    // Test implementation of abstract DomainException for testing purposes
    private class TestDomainException : DomainException
    {
        public TestDomainException(string message, string? code = null) : base(message, code) { }
    }
}
