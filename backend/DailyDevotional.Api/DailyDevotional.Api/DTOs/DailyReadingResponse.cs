using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.DTOs;

public class DailyReadingResponse
{
  public int Id { get; set; }
  public DateOnly Date {  get; set; }
  public string Book {  get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public List<DailyReadingVerseResponse> Verses { get; set; } = new();
  public string Commentary { get; set; } = string.Empty;
}
