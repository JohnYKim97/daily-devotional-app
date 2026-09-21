using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services.IServices;

public interface IUserSettingsService
{
  Task<UserSettingsResponse> GetSettingsAsync(string userId);
  Task<UserSettingsResponse> UpdateSettingsAsync(string userId, UpdateUserSettingsRequest request);
}
