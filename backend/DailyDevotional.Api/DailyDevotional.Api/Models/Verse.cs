namespace DailyDevotional.Api.Models;

public class Verse
{
  public int TranslationId { get; set; }
  public int BookId { get; set; }
  public int Chapter { get; set; }
  public int VerseNumber { get; set; }
  public string Text { get; set; } = string.Empty;
  // Section heading shown above this verse, when the translation provides one.
  // Multiple lines are separated by a newline (a Psalm's title, then its superscription).
  public string? Heading { get; set; }
  // Footnote bodies. The verse text contains [[n]] markers, where n is the 1-based
  // position in this list.
  public List<string> Footnotes { get; set; } = [];
  // Set only for translations in Cache mode; used to expire and evict rows.
  public DateTime? FetchedAt { get; set; }
  public Translation Translation { get; set; } = null!;
  public Book Book { get; set; } = null!;
}
