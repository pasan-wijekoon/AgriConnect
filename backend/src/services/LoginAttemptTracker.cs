namespace AgriConnect.Api.Services;

/// <summary>
/// Slows down password guessing (SEC-06 / OWASP A07). After <see cref="MaxFailures"/> failed sign-ins
/// for the same account from the same client within <see cref="Window"/>, further attempts are
/// refused until the window since the first failure has passed. A successful sign-in clears the
/// record. The key is account + client address, so one client cannot lock a real user out of
/// their account from somewhere else. State is in memory: it is per API instance and resets on restart.
/// </summary>
public class LoginAttemptTracker(TimeProvider? clock = null)
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly Dictionary<string, (int Failures, DateTimeOffset First)> _attempts = new();
    private readonly object _gate = new();

    /// <summary>How long the caller must wait before trying again, or null when attempts are allowed.</summary>
    public TimeSpan? RetryAfter(string email, string client)
    {
        lock (_gate)
        {
            var key = Key(email, client);
            if (!_attempts.TryGetValue(key, out var entry))
            {
                return null;
            }

            var resetsAt = entry.First + Window;
            var now = _clock.GetUtcNow();
            if (now >= resetsAt)
            {
                _attempts.Remove(key);
                return null;
            }

            return entry.Failures >= MaxFailures ? resetsAt - now : null;
        }
    }

    public void RecordFailure(string email, string client)
    {
        lock (_gate)
        {
            var key = Key(email, client);
            var now = _clock.GetUtcNow();
            if (_attempts.TryGetValue(key, out var entry) && now < entry.First + Window)
            {
                _attempts[key] = (entry.Failures + 1, entry.First);
            }
            else
            {
                _attempts[key] = (1, now);
            }
        }
    }

    public void Reset(string email, string client)
    {
        lock (_gate)
        {
            _attempts.Remove(Key(email, client));
        }
    }

    private static string Key(string email, string client) => $"{email.Trim().ToLowerInvariant()}|{client}";
}
