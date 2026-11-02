using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.SharedKernel.Middleware;

/// Central error handling. Wraps every request, catches any exception thrown further
/// down the pipeline, and returns one consistent JSON format (RFC 7807 ProblemDetails).
/// Unexpected errors are logged in full on the server, but the client only ever gets a
/// generic message, so no sensitive data can leak through an error response.
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IProblemDetailsService problemDetails)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected, so there is nobody left to answer.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, title, detail) = Describe(ex);

            if (status >= StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogInformation("{Status} on {Method} {Path}", status, context.Request.Method, context.Request.Path);

            context.Response.StatusCode = status;
            await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = ex,
                ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
            });
        }
    }

    private static (int Status, string Title, string Detail) Describe(Exception ex) => ex switch
    {
        AppException app => (app.StatusCode, app.Title, app.Message),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
            "This record was changed by someone else. Reload it and try again."),
        _ => (StatusCodes.Status500InternalServerError, "Server error",
            "Something went wrong. Please try again."),
    };
}
