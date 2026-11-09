using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Reporting.Api.Dtos;
using SwiftDrop.Reporting.Contracts;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = Roles.DispatcherOrAdmin)]
[Produces("application/json")]
public sealed class ReportsController(IReportService reports, IValidator<ReportRangeRequest> validator) : ControllerBase
{
    [HttpGet("deliveries-per-day")]
    public async Task<ActionResult<DeliveriesPerDayResponse>> DeliveriesPerDay([FromQuery] ReportRangeRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, ct);
        return Ok(Resolve(await reports.GetDeliveriesPerDayAsync(request.From, request.To, ct)));
    }

    [HttpGet("success-failure-rate")]
    public async Task<ActionResult<SuccessFailureRateResponse>> SuccessFailureRate([FromQuery] ReportRangeRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, ct);
        return Ok(Resolve(await reports.GetSuccessFailureRateAsync(request.From, request.To, ct)));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportingSummaryResponse>> Summary(CancellationToken ct) => Ok(await reports.GetSummaryAsync(ct));

    private async Task ValidateAsync(ReportRangeRequest request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid) throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }

    private static T Resolve<T>(ReportResult<T> result) where T : class => result.IsSuccess
        ? result.Value!
        : throw new BadRequestException("The report date range is invalid.");
}
