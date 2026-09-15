using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services.IServices;

public interface IJournalService
{
  Task<JournalResponse> CreateJournalAsync(string userId, CreateJournalRequest request);
  Task<JournalResponse?> GetJournalByDateAsync(string userId, DateOnly date);
  Task<JournalResponse?> UpdateJournalAsync(string userId, DateOnly date, UpdateJournalRequest request);
  Task<List<JournalHistoryEntryResponse>> GetAllJournalsAsync(string userId);
}
