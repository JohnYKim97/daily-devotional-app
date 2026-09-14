using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.Services.IServices;

public interface IBibleService
{
  Task<List<DailyReadingVerse>> GetVersesAsync(
    string book,
    int chapter,
    int startVerse,
    int endVerse);

  Task<int> GetChapterVerseCountAsync(string book, int chapter);
}
