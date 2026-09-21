namespace DailyDevotional.Api.DTOs;

public class UserSettingsResponse
{
  public bool ShowFavoriteVerseNotes { get; set; }
  public string Theme { get; set; } = "system";
}
