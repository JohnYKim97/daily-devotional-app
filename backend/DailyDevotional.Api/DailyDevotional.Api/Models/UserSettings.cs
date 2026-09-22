using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(UserId), IsUnique = true)]
public class UserSettings
{
  public int Id { get; set; }
  public string UserId { get; set; } = string.Empty;
  public bool ShowFavoriteVerseInNotes { get; set; } = false;
  public bool ShareHistory { get; set; } = true;
  public bool ShareFavoriteVerseInHistory { get; set; } = false;
  public bool EnableSearchInHistory { get; set; } = false;
  public bool EnableAiCommentary { get; set; } = false;
  public string Theme { get; set; } = "system";
}
