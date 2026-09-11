namespace DailyDevotional.Api.DTOs;

public class ImportReadingsResponse
{
  public DateOnly StartDate { get; set; }
  public DateOnly EndDate { get; set; }
  public int ImportedCount { get; set; }
  public List<DateOnly> SkippedDates { get; set; } = new();
}
