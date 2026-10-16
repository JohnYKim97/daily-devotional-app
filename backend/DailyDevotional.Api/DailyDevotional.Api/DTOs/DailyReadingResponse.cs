using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.DTOs;

public class DailyReadingResponse
{
  public int Id { get; set; }
  public DateOnly Date {  get; set; }
  public int BookId { get; set; }
  public string Book {  get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int EndChapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public List<DailyReadingVerseResponse> Verses { get; set; } = new();
  public string Commentary { get; set; } = string.Empty;
  public string Reference { get; set; } = string.Empty;
  public bool CoversWholeChapters { get; set; }
  // Why the verses are missing, when they are: "rate_limited", "too_long" (over the translation's display limit) or "error". Null otherwise.
  public string? VersesUnavailable { get; set; }
  public string TranslationCode { get; set; } = string.Empty;
  public string CopyrightNotice { get; set; } = string.Empty;
  // A link the license requires beside the copyright notice, e.g. to the publisher's site.
  public string? AttributionLinkText { get; set; }
  public string? AttributionLinkUrl { get; set; }
}
