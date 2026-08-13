namespace DailyDevotional.Api.DTOs;

public class BibleApiVerse
{
  public string Book_Id { get; set; } = string.Empty;
  public string Book_Name { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int Verse { get; set; }
  public string Text { get; set; } = string.Empty;
}
