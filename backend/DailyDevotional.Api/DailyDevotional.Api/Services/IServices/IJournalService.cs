using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services.IServices;

public interface IJournalService
{
  Task<JournalResponse> CreateJournalAsync(CreateJournalRequest request);

  Task<JournalResponse> GetJournalByDateAsync(DateOnly date);
  Task<JournalResponse> UpdateJournalAsync(DateOnly date, UpdateJournalRequest request);
}
