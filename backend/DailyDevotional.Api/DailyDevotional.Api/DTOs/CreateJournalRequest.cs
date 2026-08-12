namespace DailyDevotional.Api.DTOs;

public class CreateJournalRequest
{
  public DateTime Date { get; set; }
  public string PassageReference { get; set; } = string.Empty;
  public int? FavoriteVerse {  get; set; }
  public string Notes { get; set; } = string.Empty;
}
