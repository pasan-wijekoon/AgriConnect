using System.ComponentModel.DataAnnotations;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Config;

/// <summary>
/// Turns the exceptions services throw for bad input or missing data into RFC 7807
/// responses. Anything else falls through to the default 500 ProblemDetails, which
/// never exposes exception details.
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
            _ => (0, null),
        };

        if (status == 0)
        {
            return false;
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Instance = context.Request.Path,
            },
        });
    }
}
