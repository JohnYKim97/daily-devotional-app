using DailyDevotional.Api.DTOs;

namespace DailyDevotional.Api.Services.IServices;

public interface IBibleMetadataService
{
  Task<List<TranslationResponse>> GetTranslationsAsync();
  Task<List<BookResponse>?> GetBooksAsync(string translationCode);
  Task<List<ChapterResponse>?> GetChaptersAsync(string translationCode, int bookId);
}
