using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

// Loads the text of translations that may be stored in full (e.g. the public-domain KJV)
// the first time the app runs, so no manual import step is needed. It runs in the
// background, so the API is available straight away; a translation shows up in the
// translation list once its import finishes. When an import fails (for example with no
// internet connection) it is logged and tried again on the next start.
//
// Set "Translations:ImportOnStartup" to false to turn this off.
public class FullTranslationBootstrapService : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly IConfiguration _configuration;
  private readonly ILogger<FullTranslationBootstrapService> _logger;

  public FullTranslationBootstrapService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FullTranslationBootstrapService> logger)
  {
    _scopeFactory = scopeFactory;
    _configuration = configuration;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (!_configuration.GetValue("Translations:ImportOnStartup", true))
    {
      return;
    }

    try
    {
      List<string> codes;

      using (var scope = _scopeFactory.CreateScope())
      {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        codes = await context.Translations
          .AsNoTracking()
          .Where(t => t.StorageMode == TranslationStorageMode.Full
            && t.ProviderKind == TranslationProviderKind.Local
            && !t.IsEnabled)
          .OrderBy(t => t.SortOrder)
          .Select(t => t.Code)
          .ToListAsync(stoppingToken);
      }

      foreach (var code in codes)
      {
        await ImportAsync(code);
      }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogError(ex, "Could not check which translations need importing.");
    }
  }

  private async Task ImportAsync(string code)
  {
    try
    {
      _logger.LogInformation("Importing {Code} for the first time...", code);

      // A fresh scope per translation, so a failed import cannot leave a broken context behind.
      using var scope = _scopeFactory.CreateScope();
      var importer = scope.ServiceProvider.GetRequiredService<TranslationImportService>();

      await importer.ImportFullTranslationAsync(code, null);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogWarning(
        ex,
        "Could not import {Code}. It stays unavailable and will be tried again the next time the API starts.",
        code);
    }
  }
}
