using FluentValidation;

namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed record RegisterCourierRequest(Guid CourierId, Guid? ZoneId, string? VehicleType, string? LicensePlate);
public sealed record UpdateCourierRequest(Guid? ZoneId, bool? IsAvailable, string? VehicleType, string? LicensePlate);
public sealed record ListCouriersRequest(Guid? ZoneId, bool? IsAvailable, int Page = 1, int PageSize = 25);

public sealed class RegisterCourierValidator : AbstractValidator<RegisterCourierRequest>
{
    public RegisterCourierValidator()
    {
        RuleFor(x => x.CourierId).NotEmpty();
        RuleFor(x => x.VehicleType).MaximumLength(64);
        RuleFor(x => x.LicensePlate).MaximumLength(32);
    }
}

public sealed class UpdateCourierValidator : AbstractValidator<UpdateCourierRequest>
{
    public UpdateCourierValidator()
    {
        RuleFor(x => x.VehicleType).MaximumLength(64);
        RuleFor(x => x.LicensePlate).MaximumLength(32);
        RuleFor(x => x).Must(x => x.ZoneId.HasValue || x.IsAvailable.HasValue
            || x.VehicleType is not null || x.LicensePlate is not null)
            .WithMessage("At least one profile field must be supplied.");
    }
}

public sealed class ListCouriersValidator : AbstractValidator<ListCouriersRequest>
{
    public ListCouriersValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
