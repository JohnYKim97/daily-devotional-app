using System.Net;
using DailyDevotional.Api.Models;
using Microsoft.Extensions.Options;

namespace DailyDevotional.Api.Services.RateLimiting;

// Thrown instead of calling a Bible provider that this app has already used up its allowance
// for. It derives from HttpRequestException so callers that already degrade gracefully when a
// provider is unreachable (e.g. showing the reading without verse text) keep doing so.
public class ProviderRateLimitException : HttpRequestException
{
  public TimeSpan RetryAfter { get; }

  public ProviderRateLimitException(string message, TimeSpan retryAfter)
    : base(message, null, HttpStatusCode.TooManyRequests)
  {
    RetryAfter = retryAfter;
  }
}

public interface IProviderRateLimiter
{
  /// <summary>Records one call to the provider, or throws <see cref="ProviderRateLimitException"/> when it would exceed a limit.</summary>
  void EnsureAllowed(TranslationProviderKind kind);
}

// Sliding-window limiter that keeps the time of each recent provider call in memory.
// State is per process: it resets on restart and is not shared between several instances.
public class ProviderRateLimiter : IProviderRateLimiter
{
  private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);
  private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
  private static readonly TimeSpan Day = TimeSpan.FromDays(1);

  private readonly Dictionary<TranslationProviderKind, ProviderLimits> _limits;
  private readonly Dictionary<TranslationProviderKind, Queue<DateTimeOffset>> _calls = new();
  private readonly TimeProvider _timeProvider;
  private readonly object _lock = new();

  public ProviderRateLimiter(IOptions<RateLimitingOptions> options, TimeProvider timeProvider)
  {
    _timeProvider = timeProvider;
    _limits = new Dictionary<TranslationProviderKind, ProviderLimits>
    {
      [TranslationProviderKind.EsvApi] = options.Value.EsvApi
    };
  }

  public void EnsureAllowed(TranslationProviderKind kind)
  {
    if (!_limits.TryGetValue(kind, out var limits))
    {
      return;
    }

    lock (_lock)
    {
      var now = _timeProvider.GetUtcNow();

      if (!_calls.TryGetValue(kind, out var calls))
      {
        calls = new Queue<DateTimeOffset>();
        _calls[kind] = calls;
      }

      while (calls.Count > 0 && now - calls.Peek() >= Day)
      {
        calls.Dequeue();
      }

      var retryAfter = TimeSpan.Zero;
      retryAfter = Max(retryAfter, WaitFor(calls, now, Minute, limits.PerMinute));
      retryAfter = Max(retryAfter, WaitFor(calls, now, Hour, limits.PerHour));
      retryAfter = Max(retryAfter, WaitFor(calls, now, Day, limits.PerDay));

      if (retryAfter > TimeSpan.Zero)
      {
        throw new ProviderRateLimitException(
          $"The {kind} call limit was reached. Try again in {Math.Ceiling(retryAfter.TotalSeconds):0} seconds.",
          retryAfter);
      }

      calls.Enqueue(now);
    }
  }

  // How long until one more call fits in the window, or zero when it already does.
  private static TimeSpan WaitFor(Queue<DateTimeOffset> calls, DateTimeOffset now, TimeSpan window, int limit)
  {
    if (limit <= 0)
    {
      return TimeSpan.Zero;
    }

    // The oldest calls come first; those inside the window are the last ones in the queue.
    var inWindow = calls.Where(call => now - call < window).ToList();

    if (inWindow.Count < limit)
    {
      return TimeSpan.Zero;
    }

    // The call that has to expire for a new one to fit.
    var blocking = inWindow[inWindow.Count - limit];
    return blocking + window - now;
  }

  private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
