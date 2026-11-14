using FluentValidation;
using SwiftDrop.Merchants.Business.Dtos;

namespace SwiftDrop.Merchants.Business.Validators;

public sealed class CreateClientCredentialValidator : AbstractValidator<CreateClientCredentialCommand>
{
    public CreateClientCredentialValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Client name is required.")
            .MaximumLength(100).WithMessage("Client name must not exceed 100 characters.");
    }
}

