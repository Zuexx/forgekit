using ForgeKit.Api.Samples;
using ForgeKit.Api.Samples.Validators;
using FluentValidation.TestHelper;

namespace ForgeKit.Api.Tests.Validators;

/// <summary>
/// Unit tests for CreateResourceCommandValidator.
/// Demonstrates how to test FluentValidation validators in isolation.
/// </summary>
public class CreateResourceCommandValidatorTests
{
    private readonly CreateResourceCommandValidator _validator = new();

    [Fact]
    public void Validator_WithEmptyName_HasValidationError()
    {
        // Arrange
        var command = new CreateResourceCommand("");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validator_WithNameTooShort_HasValidationError()
    {
        // Arrange
        var command = new CreateResourceCommand("AB");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be at least 3 characters");
    }

    [Fact]
    public void Validator_WithNameTooLong_HasValidationError()
    {
        // Arrange
        var command = new CreateResourceCommand(new string('A', 101));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 100 characters");
    }

    [Fact]
    public void Validator_WithInvalidCharactersInName_HasValidationError()
    {
        // Arrange
        var command = new CreateResourceCommand("Test@Resource!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_WithValidName_PassesValidation()
    {
        // Arrange
        var command = new CreateResourceCommand("Valid-Resource_Name");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_WithCompletelyValidData_PassesAllValidation()
    {
        // Arrange
        var command = new CreateResourceCommand("Valid-Resource-Name");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
