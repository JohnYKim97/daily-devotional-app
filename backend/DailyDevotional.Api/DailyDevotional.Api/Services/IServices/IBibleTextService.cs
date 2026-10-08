using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services.IServices;

public interface IBibleTextService
{
  /// <summary>
  /// Returns a passage in the given translation (the default translation when
  /// <paramref name="translationCode"/> is null). A null <paramref name="startVerse"/> starts at
  /// verse 1 and a null <paramref name="endVerse"/> runs to the end of the end chapter. Throws <see cref="ArgumentException"/>
  /// for an unknown translation or an invalid or oversized range, and
  /// <see cref="HttpRequestException"/> when a provider cannot be reached.
  /// </summary>
  Task<PassageResponse> GetPassageAsync(
    string? translationCode,
    int bookId,
    int startChapter,
    int? startVerse,
    int endChapter,
    int? endVerse,
    bool forceRefresh = false);

  /// <summary>Deletes expired cached verses and evicts the oldest ones over the license caps.</summary>
  Task EnforceCacheLimitsAsync();
}
