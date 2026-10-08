using DailyDevotional.Api.Data;
using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

public class BibleMetadataService : IBibleMetadataService
{
  private readonly AppDbContext _context;

  public BibleMetadataService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<List<TranslationResponse>> GetTranslationsAsync()
  {
    return await _context.Translations
      .AsNoTracking()
      .Where(t => t.IsEnabled)
      .OrderBy(t => t.SortOrder)
      .Select(t => new TranslationResponse
      {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        Language = t.Language,
        CopyrightNotice = t.CopyrightNotice
      })
      .ToListAsync();
  }

  public async Task<List<BookResponse>?> GetBooksAsync(string translationCode)
  {
    var translationId = await FindEnabledTranslationIdAsync(translationCode);

    if (translationId == null)
    {
      return null;
    }

    return await _context.TranslationBooks
      .AsNoTracking()
      .Where(tb => tb.TranslationId == translationId)
      .OrderBy(tb => tb.BookId)
      .Select(tb => new BookResponse
      {
        Id = tb.BookId,
        Code = tb.Book.Code,
        Name = tb.Book.Name,
        Testament = tb.Book.Testament,
        ChapterCount = tb.ChapterCount
      })
      .ToListAsync();
  }

  public async Task<List<ChapterResponse>?> GetChaptersAsync(string translationCode, int bookId)
  {
    var translationId = await FindEnabledTranslationIdAsync(translationCode);

    if (translationId == null)
    {
      return null;
    }

    var chapters = await _context.TranslationChapters
      .AsNoTracking()
      .Where(tc => tc.TranslationId == translationId && tc.BookId == bookId)
      .OrderBy(tc => tc.Chapter)
      .Select(tc => new ChapterResponse
      {
        Chapter = tc.Chapter,
        VerseCount = tc.VerseCount
      })
      .ToListAsync();

    return chapters.Count == 0 ? null : chapters;
  }

  private async Task<int?> FindEnabledTranslationIdAsync(string translationCode)
  {
    return await _context.Translations
      .Where(t => t.Code == translationCode && t.IsEnabled)
      .Select(t => (int?)t.Id)
      .FirstOrDefaultAsync();
  }
}
