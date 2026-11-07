using FluentValidation;

namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed class ListUnassignedValidator : AbstractValidator<ListUnassignedRequest>
{
    public ListUnassignedValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("The from date must be earlier than or equal to the to date.");
    }
}
