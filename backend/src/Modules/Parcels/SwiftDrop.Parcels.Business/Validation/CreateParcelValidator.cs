using FluentValidation;
using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.Business.Validation;

public sealed class CreateParcelValidator : AbstractValidator<CreateParcelRequest>
{
    public CreateParcelValidator()
    {
        RuleFor(x => x.ZoneId).NotEmpty().WithMessage("Zone is required.");

        RuleFor(x => x.PickupAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PickupContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PickupContactPhone).NotEmpty().Matches(@"^\+?[0-9]{7,15}$")
            .WithMessage("Invalid pickup contact phone number.");

        RuleFor(x => x.DropoffAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RecipientPhone).NotEmpty().Matches(@"^\+?[0-9]{7,15}$")
            .WithMessage("Invalid recipient phone number.");

        RuleFor(x => x.WeightKg).GreaterThan(0).LessThanOrEqualTo(50)
            .WithMessage("Weight must be greater than 0 and less than or equal to 50 kg.");

        RuleFor(x => x.SizeCategory).NotEmpty();
    }
}