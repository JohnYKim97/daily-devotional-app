namespace DailyDevotional.Api.Models;

public class Journal
{
  public int Id { get; set; }
  public DateTime Date { get; set; }
  public string PassageReference { get; set; } = string.Empty;
  public int? FavoriteVerse { get; set; }
  public string Notes { get; set; } = string.Empty;


}
