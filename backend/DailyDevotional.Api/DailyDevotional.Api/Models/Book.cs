using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Models;

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Name), IsUnique = true)]
public class Book
{
  // Canonical order, 1 (Genesis) to 66 (Revelation). Matches the book ids used by bolls.life.
  public int Id { get; set; }
  public string Code { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Testament { get; set; } = string.Empty;
}
