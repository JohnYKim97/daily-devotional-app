namespace DailyDevotional.Api.Services.RateLimiting;

// Bound from the "RateLimiting" configuration section; every value has a default, so the
// section is optional.
public class RateLimitingOptions
{
  // Requests per client (signed-in user, or IP address when anonymous) per minute to the
  // endpoints that return Bible text. Cache hits count too.
  public int PassageRequestsPerMinute { get; set; } = 60;

  // Calls this app makes to the ESV API. Kept below Crossway's published limits
  // (60 per minute, 1,000 per hour, 5,000 per day) to leave headroom.
  public ProviderLimits EsvApi { get; set; } = new() { PerMinute = 50, PerHour = 900, PerDay = 4500 };
}

public class ProviderLimits
{
  public int PerMinute { get; set; }
  public int PerHour { get; set; }
  public int PerDay { get; set; }
}
