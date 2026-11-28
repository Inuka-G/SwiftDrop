using System.Text.RegularExpressions;
using FluentValidation;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Business.Validators;

public sealed class RegisterMerchantValidator : AbstractValidator<RegisterMerchantCommand>
{
    private static readonly string[] AllowedRegTypes =
        [MerchantRegType.Brn, MerchantRegType.Crn, MerchantRegType.Tin];

    private static readonly Regex TinRegex = new("^[0-9]{9}$", RegexOptions.Compiled);
    private static readonly Regex GeneralRegCharsRegex = new(@"^[A-Za-z0-9/\-._\s()]+$", RegexOptions.Compiled);

    public RegisterMerchantValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(120).WithMessage("Business name must not exceed 120 characters.");

        RuleFor(x => x.BusinessRegType)
            .NotEmpty().WithMessage("Registration type is required.")
            .Must(type => AllowedRegTypes.Contains(type?.Trim().ToUpperInvariant()))
            .WithMessage("Registration type must be BRN, CRN (Company Registration Number), or TIN.");

        RuleFor(x => x.BusinessRegNumber)
            .Cascade(CascadeMode.Stop)
            .Must(val => !string.IsNullOrWhiteSpace(val))
            .WithMessage(x => GetRequiredMessage(x.BusinessRegType))
            .MaximumLength(60).WithMessage("Registration number must not exceed 60 characters.")
            .Custom((regNumber, context) =>
            {
                var trimmed = regNumber.Trim();
                var regType = context.InstanceToValidate.BusinessRegType?.Trim().ToUpperInvariant() ?? MerchantRegType.Brn;

                if (regType == MerchantRegType.Tin)
                {
                    if (!TinRegex.IsMatch(trimmed))
                    {
                        context.AddFailure("BusinessRegNumber", "Tax Identification Number (TIN) must contain exactly 9 digits (0-9).");
                    }
                }
                else if (regType == MerchantRegType.Crn)
                {
                    if (!GeneralRegCharsRegex.IsMatch(trimmed))
                    {
                        context.AddFailure("BusinessRegNumber", "Company Registration Number contains invalid special characters.");
                    }
                }
                else
                {
                    // BRN
                    if (!GeneralRegCharsRegex.IsMatch(trimmed))
                    {
                        context.AddFailure("BusinessRegNumber", "Business Registration Number (BRN) contains invalid special characters.");
                    }
                }
            });

        RuleFor(x => x.WebhookUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Webhook URL must be a valid absolute URL (e.g., https://example.com/webhook).");
    }

    private static string GetRequiredMessage(string? regType) =>
        (regType?.Trim().ToUpperInvariant()) switch
        {
            MerchantRegType.Tin => "Tax Identification Number (TIN) is required.",
            MerchantRegType.Crn => "Company Registration Number is required.",
            _ => "Business Registration Number (BRN) is required."
        };
}
