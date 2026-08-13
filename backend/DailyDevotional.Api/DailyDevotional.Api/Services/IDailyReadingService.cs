using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services;

public interface IDailyReadingService
{
  Task<DailyReadingResponse?> GetReadingByDateAsync(DateOnly date);
  Task<bool> ImportVersesAsync(int readingId);
}
