using FluentValidation;

namespace SwiftDrop.Dispatch.Api.Dtos;

public sealed record AssignCourierRequest(Guid CourierId);

public sealed class AssignCourierValidator : AbstractValidator<AssignCourierRequest>
{
    public AssignCourierValidator() => RuleFor(x => x.CourierId).NotEmpty();
}
