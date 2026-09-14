using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.Services.IServices;

public interface IDailyReadingImportService
{
  List<ParsedReading> ParseDocument(string filePath);

  List<string> ValidateReadings(
      List<ParsedReading> readings);

  Task ResolveVerseRangesAsync(
      List<ParsedReading> readings);

  List<DailyReading> CreateDailyReadings(
      List<ParsedReading> readings,
      DateOnly startDate);
}
