using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(Date), IsUnique = true)]
public class Journal
{
  public int Id { get; set; }
  public DateOnly Date { get; set; }
  public string PassageReference { get; set; } = string.Empty;
  public int? FavoriteVerse { get; set; }
  public string Notes { get; set; } = string.Empty;


}
