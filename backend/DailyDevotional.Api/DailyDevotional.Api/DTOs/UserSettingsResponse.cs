namespace DailyDevotional.Api.DTOs;

public class UserSettingsResponse
{
  public bool ShowFavoriteVerseInNotes { get; set; }
  public string Theme { get; set; } = "system";
}
