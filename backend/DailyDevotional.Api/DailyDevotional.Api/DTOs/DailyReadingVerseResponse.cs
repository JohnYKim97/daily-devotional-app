namespace DailyDevotional.Api.DTOs;

public class DailyReadingVerseResponse
{
  public int Chapter { get; set; }
  public int Number { get; set; }
  public string Text { get; set; } = string.Empty;
}
