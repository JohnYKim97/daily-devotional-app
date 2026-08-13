namespace DailyDevotional.Api.DTOs;

public class BibleApiResponse
{
  public string Reference { get; set; } = string.Empty;
  public List<BibleApiVerse> Verses { get; set; } = new();
}
