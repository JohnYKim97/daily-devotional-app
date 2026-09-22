namespace DailyDevotional.Api.DTOs;

public class UserSettingsResponse
{
  public bool ShowFavoriteVerseInNotes { get; set; }
  public bool ShareHistory { get; set; }
  public bool ShareFavoriteVerseInHistory { get; set; }
  public bool EnableSearchInHistory { get; set; }
  public bool EnableAiCommentary { get; set; }
  public string Theme { get; set; } = "system";
}
