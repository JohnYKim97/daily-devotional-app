namespace DailyDevotional.Api.DTOs;

public class DailyReadingSummaryResponse
{
  public int Id { get; set; }
  public DateOnly Date { get; set; }
  public string Book { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int EndChapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public bool HasNotes { get; set; }
}
