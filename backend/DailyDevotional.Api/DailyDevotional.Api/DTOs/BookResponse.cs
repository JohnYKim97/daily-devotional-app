namespace DailyDevotional.Api.DTOs;

public class BookResponse
{
  public int Id { get; set; }
  public string Code { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Testament { get; set; } = string.Empty;
  public int ChapterCount { get; set; }
}
