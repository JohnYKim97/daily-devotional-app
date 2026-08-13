namespace DailyDevotional.Api.Models;

public class DailyReadingVerse
{
  public int Id { get; set; }
  public int DailyReadingId { get; set; }
  public int VerseNumber { get; set; }
  public string Text { get; set; } = string.Empty;
  public DailyReading DailyReading { get; set; } = null;
}
