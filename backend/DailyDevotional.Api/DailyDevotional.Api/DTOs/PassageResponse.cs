namespace DailyDevotional.Api.DTOs;

public class PassageResponse
{
  public string TranslationCode { get; set; } = string.Empty;
  public string CopyrightNotice { get; set; } = string.Empty;
  public int BookId { get; set; }
  public string Book { get; set; } = string.Empty;
  public string Reference { get; set; } = string.Empty;
  // True when the passage runs from verse 1 of its first chapter to the last verse of its last chapter.
  public bool CoversWholeChapters { get; set; }
  public List<DailyReadingVerseResponse> Verses { get; set; } = new();
}
