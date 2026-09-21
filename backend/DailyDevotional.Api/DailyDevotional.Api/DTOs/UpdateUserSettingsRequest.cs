namespace DailyDevotional.Api.DTOs;

public class UpdateUserSettingsRequest
{
  public bool ShowFavoriteVerseNotes { get; set; }
  public string Theme { get; set; } = "system";
}
