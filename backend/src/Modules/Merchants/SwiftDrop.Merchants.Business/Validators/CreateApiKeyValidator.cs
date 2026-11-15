using FluentValidation;
using SwiftDrop.Merchants.Business.Dtos;

namespace SwiftDrop.Merchants.Business.Validators;

public sealed class CreateApiKeyValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key nickname is required.")
            .MaximumLength(100).WithMessage("API key nickname must not exceed 100 characters.");
    }
}

