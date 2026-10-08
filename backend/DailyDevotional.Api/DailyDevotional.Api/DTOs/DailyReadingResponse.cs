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
  // Why the verses are missing, when they are: "rate_limited" or "error". Null otherwise.
  public string? VersesUnavailable { get; set; }
  public string TranslationCode { get; set; } = string.Empty;
  public string CopyrightNotice { get; set; } = string.Empty;
}
