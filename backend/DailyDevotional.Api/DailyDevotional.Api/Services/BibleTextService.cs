using DailyDevotional.Api.Data;
using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

public class BibleTextService : IBibleTextService
{
  private readonly AppDbContext _context;
  private readonly IEnumerable<ITranslationProvider> _providers;

  public BibleTextService(AppDbContext context, IEnumerable<ITranslationProvider> providers)
  {
    _context = context;
    _providers = providers;
  }

  public async Task<PassageResponse> GetPassageAsync(
    string? translationCode,
    int bookId,
    int startChapter,
    int? startVerse,
    int endChapter,
    int? endVerse,
    bool forceRefresh = false,
    bool clampVerses = false)
  {
    var code = translationCode ?? Translation.DefaultCode;

    var translation = await _context.Translations
      .AsNoTracking()
      .FirstOrDefaultAsync(t => t.Code == code && t.IsEnabled)
      ?? throw new ArgumentException($"Unknown translation '{code}'.");

    var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId)
      ?? throw new ArgumentException($"Unknown book {bookId}.");

    var chapterVerseCounts = await GetChapterVerseCountsAsync(translation, book, startChapter, endChapter);

    // A missing start means the first verse of the chapter, a missing end the last verse
    // of the end chapter.
    var firstVerse = startVerse ?? 1;
    var lastVerse = endVerse ?? chapterVerseCounts[endChapter];

    if (clampVerses)
    {
      // The schedule's verse numbers follow the ESV; a translation that numbers a chapter
      // differently (e.g. the KJV has 14 verses in 3 John, the ESV 15) ends where it ends.
      firstVerse = Math.Min(firstVerse, chapterVerseCounts[startChapter]);
      lastVerse = Math.Min(lastVerse, chapterVerseCounts[endChapter]);
    }

    ValidateVerses(chapterVerseCounts, startChapter, firstVerse, endChapter, lastVerse);

    var verses = await LoadStoredVersesAsync(translation, bookId, startChapter, firstVerse, endChapter, lastVerse);

    if (translation.StorageMode == TranslationStorageMode.Cache
      && (forceRefresh || !IsComplete(verses, chapterVerseCounts, startChapter, firstVerse, endChapter, lastVerse)))
    {
      verses = await FetchAndCacheAsync(translation, book, chapterVerseCounts, startChapter, firstVerse, endChapter, lastVerse);
    }

    var bookChapterCount = await _context.TranslationBooks
      .AsNoTracking()
      .Where(tb => tb.TranslationId == translation.Id && tb.BookId == book.Id)
      .Select(tb => tb.ChapterCount)
      .FirstAsync();

    return new PassageResponse
    {
      Reference = PassageReferenceFormatter.Format(
        book.Name,
        bookChapterCount,
        startChapter,
        firstVerse,
        endChapter,
        lastVerse,
        chapterVerseCounts[endChapter]),
      CoversWholeChapters = firstVerse == 1 && lastVerse == chapterVerseCounts[endChapter],
      TranslationCode = translation.Code,
      CopyrightNotice = translation.CopyrightNotice,
      BookId = book.Id,
      Book = book.Name,
      Verses = verses
        .Select(v => new DailyReadingVerseResponse
        {
          Chapter = v.Chapter,
          Number = v.VerseNumber,
          Text = v.Text,
          Heading = v.Heading,
          Footnotes = v.Footnotes
        })
        .ToList()
    };
  }

  private async Task<Dictionary<int, int>> GetChapterVerseCountsAsync(
    Translation translation,
    Book book,
    int startChapter,
    int endChapter)
  {
    if (startChapter < 1 || endChapter < startChapter)
    {
      throw new ArgumentException("Invalid passage range.");
    }

    var counts = await _context.TranslationChapters
      .AsNoTracking()
      .Where(tc => tc.TranslationId == translation.Id
        && tc.BookId == book.Id
        && tc.Chapter >= startChapter
        && tc.Chapter <= endChapter)
      .ToDictionaryAsync(tc => tc.Chapter, tc => tc.VerseCount);

    if (counts.Count != endChapter - startChapter + 1)
    {
      throw new ArgumentException($"{book.Name} does not have chapters {startChapter}-{endChapter} in {translation.Code}.");
    }

    return counts;
  }

  private static void ValidateVerses(
    Dictionary<int, int> chapterVerseCounts,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    if (startVerse < 1 || endVerse < 1 || (startChapter == endChapter && endVerse < startVerse))
    {
      throw new ArgumentException("Invalid passage range.");
    }

    if (startVerse > chapterVerseCounts[startChapter] || endVerse > chapterVerseCounts[endChapter])
    {
      throw new ArgumentException("Verse is outside the chapter.");
    }
  }

  private async Task<List<Verse>> LoadStoredVersesAsync(
    Translation translation,
    int bookId,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    var query = _context.Verses
      .AsNoTracking()
      .Where(v => v.TranslationId == translation.Id
        && v.BookId == bookId
        && v.Chapter >= startChapter
        && v.Chapter <= endChapter
        && (v.Chapter > startChapter || v.VerseNumber >= startVerse)
        && (v.Chapter < endChapter || v.VerseNumber <= endVerse));

    if (translation.StorageMode == TranslationStorageMode.Cache && translation.MaxCacheAgeDays is int days)
    {
      var cutoff = DateTime.UtcNow.AddDays(-days);
      query = query.Where(v => v.FetchedAt >= cutoff);
    }

    return await query
      .OrderBy(v => v.Chapter)
      .ThenBy(v => v.VerseNumber)
      .ToListAsync();
  }

  // A cached range counts as complete when every chapter in it has rows from
  // the first to the last expected verse. A gap of one number is tolerated
  // because some translations skip a verse number (e.g. ESV Acts 8:37).
  private static bool IsComplete(
    List<Verse> verses,
    Dictionary<int, int> chapterVerseCounts,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    for (var chapter = startChapter; chapter <= endChapter; chapter++)
    {
      var first = chapter == startChapter ? startVerse : 1;
      var last = chapter == endChapter ? endVerse : chapterVerseCounts[chapter];

      var numbers = verses
        .Where(v => v.Chapter == chapter)
        .Select(v => v.VerseNumber)
        .ToList();

      if (numbers.Count == 0 || numbers[0] > first + 1 || numbers[^1] < last - 1)
      {
        return false;
      }

      for (var i = 1; i < numbers.Count; i++)
      {
        if (numbers[i] - numbers[i - 1] > 2)
        {
          return false;
        }
      }
    }

    return true;
  }

  private async Task<List<Verse>> FetchAndCacheAsync(
    Translation translation,
    Book book,
    Dictionary<int, int> chapterVerseCounts,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    var provider = _providers.FirstOrDefault(p => p.Kind == translation.ProviderKind)
      ?? throw new InvalidOperationException($"No provider is configured for {translation.Code}.");

    var requestedVerses = Enumerable.Range(startChapter, endChapter - startChapter + 1)
      .Sum(chapter =>
      {
        var first = chapter == startChapter ? startVerse : 1;
        var last = chapter == endChapter ? endVerse : chapterVerseCounts[chapter];
        return last - first + 1;
      });

    var limit = await GetBookCacheLimitAsync(translation, book.Id);

    if (requestedVerses > limit)
    {
      throw new ArgumentException(
        $"{translation.Code} passages are limited to {limit} verses for {book.Name} under its license.");
    }

    var fetched = await provider.GetVersesAsync(book.Name, startChapter, startVerse, endChapter, endVerse);

    if (fetched.Count == 0)
    {
      return [];
    }

    var now = DateTime.UtcNow;

    var existing = await _context.Verses
      .Where(v => v.TranslationId == translation.Id
        && v.BookId == book.Id
        && v.Chapter >= startChapter
        && v.Chapter <= endChapter)
      .ToDictionaryAsync(v => (v.Chapter, v.VerseNumber));

    var stored = new List<Verse>();

    foreach (var item in fetched)
    {
      if (existing.TryGetValue((item.Chapter, item.VerseNumber), out var verse))
      {
        verse.Text = item.Text;
        verse.Heading = item.Heading;
        verse.Footnotes = item.Footnotes ?? [];
        verse.FetchedAt = now;
      }
      else
      {
        verse = new Verse
        {
          TranslationId = translation.Id,
          BookId = book.Id,
          Chapter = item.Chapter,
          VerseNumber = item.VerseNumber,
          Text = item.Text,
          Heading = item.Heading,
          Footnotes = item.Footnotes ?? [],
          FetchedAt = now
        };
        _context.Verses.Add(verse);
      }

      stored.Add(verse);
    }

    try
    {
      await _context.SaveChangesAsync();
      await EnforceCacheLimitsAsync(translation);
    }
    catch (DbUpdateException)
    {
      // A concurrent request cached the same verses first. The fetched text is
      // still valid to return, so don't fail this request.
      _context.ChangeTracker.Clear();
    }

    return stored
      .OrderBy(v => v.Chapter)
      .ThenBy(v => v.VerseNumber)
      .ToList();
  }

  public async Task EnforceCacheLimitsAsync()
  {
    var translations = await _context.Translations
      .AsNoTracking()
      .Where(t => t.StorageMode == TranslationStorageMode.Cache)
      .ToListAsync();

    foreach (var translation in translations)
    {
      await EnforceCacheLimitsAsync(translation);
    }
  }

  private async Task EnforceCacheLimitsAsync(Translation translation)
  {
    if (translation.StorageMode != TranslationStorageMode.Cache)
    {
      return;
    }

    if (translation.MaxCacheAgeDays is int days)
    {
      var cutoff = DateTime.UtcNow.AddDays(-days);

      await _context.Verses
        .Where(v => v.TranslationId == translation.Id && v.FetchedAt < cutoff)
        .ExecuteDeleteAsync();
    }

    if (translation.MaxCachedVerses is not int maxCachedVerses)
    {
      return;
    }

    // Per-book cap first (at most half of any book), then the overall cap.
    var countsByBook = await _context.Verses
      .Where(v => v.TranslationId == translation.Id)
      .GroupBy(v => v.BookId)
      .Select(g => new { BookId = g.Key, Count = g.Count() })
      .ToListAsync();

    foreach (var bookCount in countsByBook)
    {
      var limit = await GetBookCacheLimitAsync(translation, bookCount.BookId);

      if (bookCount.Count > limit)
      {
        await EvictOldestAsync(translation.Id, bookCount.BookId, bookCount.Count - limit);
      }
    }

    var total = await _context.Verses.CountAsync(v => v.TranslationId == translation.Id);

    if (total > maxCachedVerses)
    {
      await EvictOldestAsync(translation.Id, null, total - maxCachedVerses);
    }
  }

  private async Task EvictOldestAsync(int translationId, int? bookId, int count)
  {
    var query = _context.Verses.Where(v => v.TranslationId == translationId);

    if (bookId != null)
    {
      query = query.Where(v => v.BookId == bookId);
    }

    var oldest = await query
      .OrderBy(v => v.FetchedAt)
      .ThenBy(v => v.BookId)
      .ThenBy(v => v.Chapter)
      .ThenBy(v => v.VerseNumber)
      .Take(count)
      .ToListAsync();

    _context.Verses.RemoveRange(oldest);
    await _context.SaveChangesAsync();
  }

  // The most verses of one book that may be cached or returned at once: the
  // overall cap, or half the book when that is smaller (one- and two-chapter
  // books are exempt from the half-book rule).
  private async Task<int> GetBookCacheLimitAsync(Translation translation, int bookId)
  {
    var max = translation.MaxCachedVerses ?? int.MaxValue;

    var chapters = await _context.TranslationChapters
      .AsNoTracking()
      .Where(tc => tc.TranslationId == translation.Id && tc.BookId == bookId)
      .Select(tc => tc.VerseCount)
      .ToListAsync();

    if (chapters.Count <= 2)
    {
      return max;
    }

    return Math.Min(max, chapters.Sum() / 2);
  }
}
