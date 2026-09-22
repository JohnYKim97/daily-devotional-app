namespace DailyDevotional.Api.DTOs;

public class UpdateUserSettingsRequest
{
  public bool ShowFavoriteVerseInNotes { get; set; }
  public bool ShareHistory { get; set; }
  public bool ShareFavoriteVerseInHistory { get; set; }
  public string Theme { get; set; } = "system";
}
