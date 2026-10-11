using System.ComponentModel.DataAnnotations;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AgriConnect.Api.Config;

/// <summary>
/// Turns the exceptions services throw for bad input or missing data into RFC 7807
/// responses, and a database that cannot be reached or has no free connection into a 503
/// with Retry-After (the request is fine, the server is temporarily saturated; DEF-P-01).
/// Anything else falls through to the default 500 ProblemDetails, which never exposes
/// exception details.
/// </summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException or ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ when IsDatabaseUnavailable(exception) => (StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable"),
            _ => (0, null),
        };

        if (status == 0)
        {
            return false;
        }

        context.Response.StatusCode = status;
        if (status == StatusCodes.Status503ServiceUnavailable)
        {
            context.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // The 503 detail is fixed text: the real exception can name hosts and pool settings.
                Detail = status == StatusCodes.Status503ServiceUnavailable
                    ? "The server is busy right now. Please try again in a few seconds."
                    : exception.Message,
                Instance = context.Request.Path,
            },
        });
    }

    private const int RetryAfterSeconds = 5;

    /// <summary>
    /// True when the failure is about reaching the database, not about the request or the data:
    /// a connection that could not be opened or timed out waiting for a pooled connection, or
    /// the server (or a pooler such as Supabase's) refusing further clients. Serialization
    /// failures (40001) and constraint violations (23xxx) are deliberately not included; the
    /// reservation retry loop and the services own those.
    /// </summary>
    internal static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case PostgresException pg:
                    if (pg.SqlState is "53300" || pg.MessageText.Contains("max clients reached", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    break;
                case NpgsqlException:
                    // Connection-level failure (PostgresException, handled above, is the server answering).
                    return true;
                case TimeoutException:
                    // Npgsql raises TimeoutException when the pool has no free connection in time.
                    return true;
            }
        }

        return false;
    }
}
