using AgriConnect.Api.Services;

namespace backend.Tests.services;

/// <summary>SEC-06: repeated failed sign-ins for one account from one client must be refused for a while.</summary>
public class LoginAttemptTrackerTests
{
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private const string Client = "10.0.0.1";

    private static void Fail(LoginAttemptTracker t, int times, string email = "a@x.lk", string client = Client)
    {
        for (var i = 0; i < times; i++) t.RecordFailure(email, client);
    }

    [Fact]
    public void BelowTheLimit_AttemptsAreAllowed()
    {
        var t = new LoginAttemptTracker(new ManualClock());
        Fail(t, LoginAttemptTracker.MaxFailures - 1);

        Assert.Null(t.RetryAfter("a@x.lk", Client));
    }

    [Fact]
    public void AtTheLimit_FurtherAttemptsAreRefusedWithAWait()
    {
        var t = new LoginAttemptTracker(new ManualClock());
        Fail(t, LoginAttemptTracker.MaxFailures);

        var wait = t.RetryAfter("a@x.lk", Client);

        Assert.NotNull(wait);
        Assert.Equal(LoginAttemptTracker.Window, wait);
    }

    [Fact]
    public void AfterTheWindow_AttemptsAreAllowedAgain()
    {
        var clock = new ManualClock();
        var t = new LoginAttemptTracker(clock);
        Fail(t, LoginAttemptTracker.MaxFailures);

        clock.Now += LoginAttemptTracker.Window;

        Assert.Null(t.RetryAfter("a@x.lk", Client));
    }

    [Fact]
    public void SuccessfulSignIn_ClearsTheFailures()
    {
        var t = new LoginAttemptTracker(new ManualClock());
        Fail(t, LoginAttemptTracker.MaxFailures - 1);

        t.Reset("a@x.lk", Client);
        Fail(t, LoginAttemptTracker.MaxFailures - 1);

        Assert.Null(t.RetryAfter("a@x.lk", Client));
    }

    [Fact]
    public void TheKeyIsAccountPlusClient_SoOtherClientsAndAccountsAreUnaffected()
    {
        var t = new LoginAttemptTracker(new ManualClock());
        Fail(t, LoginAttemptTracker.MaxFailures);

        Assert.NotNull(t.RetryAfter("A@X.LK ", Client)); // same account, case/space-insensitive
        Assert.Null(t.RetryAfter("a@x.lk", "10.0.0.2")); // the real user on another address
        Assert.Null(t.RetryAfter("b@x.lk", Client));     // another account from the attacker's address
    }
}
