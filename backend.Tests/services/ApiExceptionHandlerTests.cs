using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace backend.Tests.services;

/// <summary>
/// DEF-P-01: under load the database pool (or a pooler in front of it) ran out of connections and
/// the API answered 500. A database that is busy or unreachable must be a 503 with Retry-After,
/// while genuine server bugs stay 500 and serialization failures stay with the reservation retry loop.
/// </summary>
public class ApiExceptionHandlerTests
{
    private sealed class CapturingProblemDetailsService : Microsoft.AspNetCore.Http.IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }

    private static async Task<(bool handled, int status, CapturingProblemDetailsService service, HttpContext ctx)> Handle(Exception ex)
    {
        var service = new CapturingProblemDetailsService();
        var ctx = new DefaultHttpContext();
        var handled = await new ApiExceptionHandler(service).TryHandleAsync(ctx, ex, CancellationToken.None);
        return (handled, ctx.Response.StatusCode, service, ctx);
    }

    [Fact]
    public async Task PoolExhaustionTimeout_Returns503WithRetryAfter()
    {
        // What Npgsql throws when no pooled connection becomes free within the timeout.
        var ex = new InvalidOperationException("transient failure",
            new NpgsqlException("connection pool exhausted", new TimeoutException("The operation has timed out.")));

        var (handled, status, service, ctx) = await Handle(ex);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("5", ctx.Response.Headers.RetryAfter.ToString());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, service.Written!.ProblemDetails.Status);
    }

    [Fact]
    public async Task DatabaseUnreachable_Returns503_WithoutLeakingTheExceptionText()
    {
        var ex = new NpgsqlException("Failed to connect to 15.165.245.138:5432", new TimeoutException());

        var (_, status, service, _) = await Handle(ex);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.DoesNotContain("15.165.245.138", service.Written!.ProblemDetails.Detail);
    }

    [Fact]
    public async Task ServerRefusingMoreClients_Returns503()
    {
        var ex = new PostgresException("max clients reached in session mode - max clients are limited to pool_size: 15", "FATAL", "FATAL", "XX000");

        var (handled, status, _, _) = await Handle(ex);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
    }

    [Fact]
    public async Task SerializationFailure_IsNotTreatedAsUnavailable()
    {
        // 40001 belongs to the stock-reservation retry loop; it must keep its existing behaviour.
        var ex = new InvalidOperationException("transient failure",
            new PostgresException("could not serialize access", "ERROR", "ERROR", "40001"));

        var (handled, _, _, _) = await Handle(ex);

        Assert.False(handled);
    }

    [Fact]
    public async Task OrdinaryServerBug_StaysAnUnhandled500()
    {
        var (handled, _, _, _) = await Handle(new NullReferenceException());

        Assert.False(handled);
    }

    [Fact]
    public async Task NotFound_StillMapsTo404()
    {
        var (handled, status, _, _) = await Handle(new NotFoundException("missing"));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, status);
    }
}
