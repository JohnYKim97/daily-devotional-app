using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services;

public interface IJournalService
{
  Task<JournalResponse> CreateJournalAsync(CreateJournalRequest request);

  Task<JournalResponse> GetJournalByDateAsync(DateTime date);
}
