using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

public enum TranslationStorageMode
{
  // Public domain or openly licensed: the whole text may live in the database.
  Full = 0,
  // Restricted: only a limited, expiring cache of fetched verses may be kept.
  Cache = 1
}

public enum TranslationProviderKind
{
  Local = 0,
  EsvApi = 1,
  NltApi = 2
}

[Index(nameof(Code), IsUnique = true)]
public class Translation
{
  public const string DefaultCode = "ESV";

  public int Id { get; set; }
  public string Code { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Language { get; set; } = "en";
  public TranslationStorageMode StorageMode { get; set; }
  public TranslationProviderKind ProviderKind { get; set; }
  public int? MaxCachedVerses { get; set; }
  public int? MaxCacheAgeDays { get; set; }
  public string CopyrightNotice { get; set; } = string.Empty;
  public bool NonCommercialOnly { get; set; }
  public bool IsEnabled { get; set; }
  public int SortOrder { get; set; }
}
