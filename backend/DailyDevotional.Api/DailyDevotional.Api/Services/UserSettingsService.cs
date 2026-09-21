using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

public class UserSettingsService : IUserSettingsService
{
  private readonly AppDbContext _context;

  public UserSettingsService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<UserSettingsResponse> GetSettingsAsync(string userId)
  {
    var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

    if (settings == null)
    {
      return new UserSettingsResponse
      {
        ShowFavoriteVerseInNotes = false,
        Theme = "system",
      };
    }

    return new UserSettingsResponse
    {
      ShowFavoriteVerseInNotes = settings.ShowFavoriteVerseInNotes,
      Theme = settings.Theme,
    };
  }

  public async Task<UserSettingsResponse> UpdateSettingsAsync(string userId, UpdateUserSettingsRequest request)
  {
    var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

    if (settings == null)
    {
      settings = new UserSettings
      {
        UserId = userId
      };
      _context.UserSettings.Add(settings);
    }

    settings.ShowFavoriteVerseInNotes = request.ShowFavoriteVerseInNotes;
    settings.Theme = request.Theme;

    await _context.SaveChangesAsync();

    return new UserSettingsResponse
    {
      ShowFavoriteVerseInNotes = settings.ShowFavoriteVerseInNotes,
      Theme = settings.Theme,
    };
  }
}
