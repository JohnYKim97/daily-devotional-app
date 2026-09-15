namespace DailyDevotional.Api.DTOs;

public class JournalHistoryEntryResponse
{
  public int Id { get; set; }
  public DateOnly Date { get; set; }
  public string Book { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public string Notes { get; set; } = string.Empty;
}
