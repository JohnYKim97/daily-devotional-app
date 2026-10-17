using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

// Periodically expires cached verses so license-restricted translations never
// keep text past their allowed age, even if nobody requests a passage.
public class TranslationCacheCleanupService : BackgroundService
{
  private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<TranslationCacheCleanupService> _logger;

  public TranslationCacheCleanupService(IServiceScopeFactory scopeFactory, ILogger<TranslationCacheCleanupService> logger)
  {
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(Interval);

    do
    {
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var bibleTextService = scope.ServiceProvider.GetRequiredService<IBibleTextService>();
        await bibleTextService.EnforceCacheLimitsAsync();
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        _logger.LogError(ex, "Translation cache cleanup failed.");
      }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
  }
}
