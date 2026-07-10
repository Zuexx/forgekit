using FluentValidation;

namespace ForgeKit.Api.Samples.Validators;

/// <summary>
/// Validator for CreateResourceCommand.
/// Demonstrates FluentValidation usage in ForgeKit.
/// 
/// This validator is automatically discovered and injected into ValidationBehavior
/// when a CreateResourceCommand request is sent through MediatR.
/// </summary>
public class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        // Name validation - required, length constraints, format rules
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.]+$")
            .WithMessage("Name can only contain letters, numbers, spaces, hyphens, underscores, and periods");
    }
}
