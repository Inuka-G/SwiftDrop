using FluentValidation;

namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed class ListParcelsValidator : AbstractValidator<ListParcelsRequest>
{
    public ListParcelsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).MaximumLength(32);
        RuleFor(x => x.TrackingNumber).MaximumLength(80);
        RuleFor(x => x.AssignmentState)
            .Must(state => state is null || new[] { "UNASSIGNED", "ASSIGNED", "COMPLETED" }
                .Contains(state.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Assignment state must be UNASSIGNED, ASSIGNED, or COMPLETED.");
    }
}
