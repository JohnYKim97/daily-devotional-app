namespace DailyDevotional.Api.Models;

public class TranslationBook
{
  public int TranslationId { get; set; }
  public int BookId { get; set; }
  public int ChapterCount { get; set; }
  public Translation Translation { get; set; } = null!;
  public Book Book { get; set; } = null!;
}
