using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.Services.IServices;

public interface IDailyReadingService
{
  Task<DailyReadingResponse?> GetReadingByDateAsync(DateOnly date);
  Task<bool> ImportVersesAsync(int readingId);
  Task<ImportReadingsResponse> SaveImportedReadingsAsync(
      List<DailyReading> readings,
      bool overwrite);
}
