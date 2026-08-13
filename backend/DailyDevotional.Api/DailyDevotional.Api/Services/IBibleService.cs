using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.Services;

public interface IBibleService
{
  Task<List<DailyReadingVerse>> GetVersesAsync(
    string book,
    int chapter,
    int startVerse,
    int endVerse);


}
