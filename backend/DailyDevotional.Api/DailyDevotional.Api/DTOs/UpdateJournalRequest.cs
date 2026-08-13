namespace DailyDevotional.Api.DTOs;

public class UpdateJournalRequest
{
    public string PassageReference { get; set; } = string.Empty;
  public int? FavoriteVerse {  get; set; }
  public string Notes { get; set; } = string.Empty;
}
