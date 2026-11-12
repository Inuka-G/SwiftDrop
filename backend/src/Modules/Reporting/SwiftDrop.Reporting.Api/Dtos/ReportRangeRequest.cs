using FluentValidation;
using SwiftDrop.Reporting.Contracts;

namespace SwiftDrop.Reporting.Api.Dtos;

public sealed record ReportRangeRequest(DateOnly? From, DateOnly? To);

public sealed class ReportRangeRequestValidator : AbstractValidator<ReportRangeRequest>
{
    public ReportRangeRequestValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("The from date is required.");
        RuleFor(x => x.To).NotNull().WithMessage("The to date is required.");
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("The from date must be on or before the to date.");
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From > x.To || x.To.Value.DayNumber - x.From.Value.DayNumber + 1 <= ReportQueryLimits.MaximumRangeDays)
            .WithMessage($"The date range cannot exceed {ReportQueryLimits.MaximumRangeDays} days.");
    }
}
