using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(Date), IsUnique = true)]
public class DailyReading
{
  public int Id { get; set; }
  public DateOnly Date { get; set; }
  public int BookId { get; set; }
  public int Chapter { get; set; }
  public int EndChapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public string Commentary { get; set; } = string.Empty;
  public Book Book { get; set; } = null!;
}
