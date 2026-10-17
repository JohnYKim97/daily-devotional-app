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
  NltApi = 2,
  // The YouVersion Platform API; Translation.ExternalId is the YouVersion Bible id.
  YouVersionApi = 3
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
  // The provider's own id for this translation (e.g. the YouVersion Bible id).
  public int? ExternalId { get; set; }
  // A license limit on how much may be shown at once: at most this many chapters, or this
  // many verses when that allows more. A passage over both limits is not shown.
  public int? MaxDisplayChapters { get; set; }
  public int? MaxDisplayVerses { get; set; }
  // A link the license requires beside the copyright notice (e.g. to the publisher).
  public string? AttributionLinkText { get; set; }
  public string? AttributionLinkUrl { get; set; }
  // The copyright notice is kept up to date from the provider instead of being fixed.
  public bool NoticeFromProvider { get; set; }
  public bool IsEnabled { get; set; }
  public int SortOrder { get; set; }
}
