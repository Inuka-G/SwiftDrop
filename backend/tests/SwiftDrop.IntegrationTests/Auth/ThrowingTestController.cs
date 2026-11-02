using Microsoft.AspNetCore.Mvc;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.IntegrationTests.Auth;

/// Exists only in tests: lets us trigger errors and inspect what the client receives.
[ApiController]
[Route("api/v1/test-errors")]
public class ThrowingTestController : ControllerBase
{
    public const string Secret = "Host=db;Password=super-secret";

    [HttpGet("unexpected")]
    public IActionResult Unexpected() => throw new InvalidOperationException(Secret);

    [HttpGet("not-found")]
    public IActionResult Missing() => throw new NotFoundException("Parcel not found.");
}
