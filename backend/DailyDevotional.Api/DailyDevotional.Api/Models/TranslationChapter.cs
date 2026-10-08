namespace DailyDevotional.Api.Models;

public class TranslationChapter
{
  public int TranslationId { get; set; }
  public int BookId { get; set; }
  public int Chapter { get; set; }
  // Highest verse number in the chapter. Some translations skip a number
  // (e.g. the ESV omits Acts 8:37), so this can exceed the stored row count.
  public int VerseCount { get; set; }
  public Translation Translation { get; set; } = null!;
  public Book Book { get; set; } = null!;
}
