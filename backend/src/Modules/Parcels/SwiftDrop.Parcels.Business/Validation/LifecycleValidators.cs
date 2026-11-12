using FluentValidation;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.Business.Validation;

public sealed class UpdateDeliveryStatusValidator : AbstractValidator<UpdateDeliveryStatusRequest>
{
    private static readonly string[] InProgressStatuses =
        [ParcelStatuses.PickedUp, ParcelStatuses.InTransit, ParcelStatuses.OutForDelivery];

    public UpdateDeliveryStatusValidator()
    {
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => InProgressStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", InProgressStatuses)}.");
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ReportFailureValidator : AbstractValidator<ReportFailureRequest>
{
    public ReportFailureValidator()
    {
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => FailureReasons.All.Contains(r))
            .WithMessage($"Reason must be one of: {string.Join(", ", FailureReasons.All)}.");
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.Note).NotEmpty()
            .When(x => x.Reason == FailureReasons.Other)
            .WithMessage("Describe what happened when the reason is Other.");
    }
}

public sealed class ConfirmDeliveryValidator : AbstractValidator<ConfirmDeliveryRequest>
{
    public ConfirmDeliveryValidator()
    {
        RuleFor(x => x.RecipientName).MaximumLength(150);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
