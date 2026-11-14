using FluentValidation;
using SwiftDrop.Merchants.Business.Dtos;

namespace SwiftDrop.Merchants.Business.Validators;

public sealed class RegisterMerchantValidator : AbstractValidator<RegisterMerchantCommand>
{
    public RegisterMerchantValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(120).WithMessage("Business name must not exceed 120 characters.");

        RuleFor(x => x.BusinessRegNumber)
            .NotEmpty().WithMessage("Business registration number is required.")
            .MinimumLength(3).WithMessage("Business registration number must be at least 3 characters.")
            .MaximumLength(60).WithMessage("Business registration number must not exceed 60 characters.");

        RuleFor(x => x.WebhookUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Webhook URL must be a valid absolute URL (e.g., https://example.com/webhook).");
    }
}

