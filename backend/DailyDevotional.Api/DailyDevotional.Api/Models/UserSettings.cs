using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(UserId), IsUnique = true)]
public class UserSettings
{
  public int Id { get; set; }
  public string UserId { get; set; } = string.Empty;
  public bool ShowFavoriteVerseNotes { get; set; } = false;
  public string Theme { get; set; } = "system";
}
