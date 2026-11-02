using Microsoft.AspNetCore.Http;

namespace SwiftDrop.SharedKernel.Exceptions;

/// An expected failure. Its message is safe to show to the client, so never put
/// personal data or internal details in it. ExceptionHandlingMiddleware turns it
/// into an HTTP response, so services simply throw and controllers stay clean.
public abstract class AppException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

public sealed class BadRequestException(string message)
    : AppException(StatusCodes.Status400BadRequest, "Bad request", message);

public sealed class UnauthorizedException(string message)
    : AppException(StatusCodes.Status401Unauthorized, "Unauthorized", message);

public sealed class ForbiddenException(string message)
    : AppException(StatusCodes.Status403Forbidden, "Forbidden", message);

public sealed class NotFoundException(string message)
    : AppException(StatusCodes.Status404NotFound, "Not found", message);

public sealed class ConflictException(string message)
    : AppException(StatusCodes.Status409Conflict, "Conflict", message);
