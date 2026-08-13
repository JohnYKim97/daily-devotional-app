using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(Date), IsUnique = true)]
public class DailyReading
{
  public int Id { get; set; }
  public DateOnly Date { get; set; }
  public string Book { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public string Commentary { get; set; } = string.Empty;
  public ICollection<DailyReadingVerse> Verses { get; set; } = new List<DailyReadingVerse>();
}
