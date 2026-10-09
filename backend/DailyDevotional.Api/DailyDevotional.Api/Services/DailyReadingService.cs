using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.RateLimiting;

namespace DailyDevotional.Api.Services;

public class DailyReadingService : IDailyReadingService
{
  public readonly AppDbContext _context;
  private readonly IBibleTextService _bibleTextService;
  private readonly IAiCommentaryService _aiCommentaryService;

  public DailyReadingService(AppDbContext context, IBibleTextService bibleTextService, IAiCommentaryService aiCommentaryService)
  {
    _context = context;
    _bibleTextService = bibleTextService;
    _aiCommentaryService = aiCommentaryService;
  }

  public async Task<DailyReadingResponse?> GetReadingByDateAsync(DateOnly date, string? translationCode = null)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Book)
      .FirstOrDefaultAsync(r => r.Date == date);

    if (reading == null)
    {
      return null;
    }

    var response = new DailyReadingResponse
    {
      Id = reading.Id,
      Date = reading.Date,
      BookId = reading.BookId,
      Book = reading.Book.Name,
      Chapter = reading.Chapter,
      EndChapter = reading.EndChapter,
      StartVerse = reading.StartVerse,
      EndVerse = reading.EndVerse,
      Commentary = reading.Commentary
    };

    try
    {
      var passage = await GetPassageAsync(reading, translationCode);

      response.Reference = passage.Reference;
      response.CoversWholeChapters = passage.CoversWholeChapters;
      response.TranslationCode = passage.TranslationCode;
      response.CopyrightNotice = passage.CopyrightNotice;
      response.Verses = passage.Verses;
    }
    catch (ProviderRateLimitException)
    {
      // This app has used up its allowance for the Bible provider for now.
      response.VersesUnavailable = "rate_limited";
    }
    catch (HttpRequestException)
    {
      // Verse text could not be fetched right now (e.g. the ESV API is
      // briefly unavailable). The rest of the reading still loads; this
      // will simply retry on the next request for this date.
      response.VersesUnavailable = "error";
    }

    return response;
  }

  public async Task<List<DailyReadingSummaryResponse>> GetScheduleAsync(string userId)
  {
    var datesWithNotes = await _context.Journals
      .Where(j => j.UserId == userId && !string.IsNullOrWhiteSpace(j.Notes))
      .Select(j => j.Date)
      .ToListAsync();

    var dateswithNotesSet = datesWithNotes.ToHashSet();

    return await _context.DailyReadings
      .OrderBy(r => r.Date)
      .Select(r => new DailyReadingSummaryResponse
      {
        Id = r.Id,
        Date = r.Date,
        BookId = r.BookId,
        Book = r.Book.Name,
        Chapter = r.Chapter,
        EndChapter = r.EndChapter,
        StartVerse = r.StartVerse,
        EndVerse = r.EndVerse,
        HasNotes = dateswithNotesSet.Contains(r.Date),
      })
      .ToListAsync();
  }

  public async Task<bool> ImportVersesAsync(int readingId)
  {
    var reading = await _context.DailyReadings.FirstOrDefaultAsync(r => r.Id == readingId);

    if (reading == null)
    {
      return false;
    }

    var passage = await GetPassageAsync(reading, null, forceRefresh: true);

    return passage.Verses.Count > 0;
  }

  private Task<PassageResponse> GetPassageAsync(DailyReading reading, string? translationCode, bool forceRefresh = false)
  {
    return _bibleTextService.GetPassageAsync(
      translationCode,
      reading.BookId,
      reading.Chapter,
      reading.StartVerse,
      // The seeded sample readings predate EndChapter and leave it at 0.
      Math.Max(reading.Chapter, reading.EndChapter),
      reading.EndVerse,
      forceRefresh,
      clampVerses: true);
  }

  public async Task<ImportReadingsResponse> SaveImportedReadingsAsync(
      List<DailyReading> readings,
      bool overwrite)
  {
    var dates = readings
      .Select(r => r.Date)
      .ToList();

    var existingReadings = await _context.DailyReadings
      .Where(r => dates.Contains(r.Date))
      .ToListAsync();

    var existingDates = existingReadings
      .Select(r => r.Date)
      .ToHashSet();

    var response = new ImportReadingsResponse
    {
      StartDate = readings.Min(r => r.Date),
      EndDate = readings.Max(r => r.Date)
    };

    if (overwrite && existingReadings.Count > 0)
    {
      _context.DailyReadings.RemoveRange(existingReadings);

      // Persist the deletes first so the unique index on Date
      // doesn't collide with the inserts below.
      await _context.SaveChangesAsync();

      existingDates.Clear();
    }
    else
    {
      response.SkippedDates = readings
        .Where(r => existingDates.Contains(r.Date))
        .Select(r => r.Date)
        .ToList();
    }

    var readingsToInsert = readings
      .Where(r => !existingDates.Contains(r.Date))
      .ToList();

    await _context.DailyReadings.AddRangeAsync(readingsToInsert);

    await _context.SaveChangesAsync();

    response.ImportedCount = readingsToInsert.Count;

    return response;
  }

  public async Task<string?> GetOrGenerateCommentaryAsync(DateOnly date)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Book)
      .FirstOrDefaultAsync(r => r.Date == date);

    if (reading == null)
    {
      return null;
    }

    if (!string.IsNullOrWhiteSpace(reading.Commentary))
    {
      return reading.Commentary;
    }

    PassageResponse passage;

    try
    {
      passage = await GetPassageAsync(reading, null);
    }
    catch (HttpRequestException)
    {
      return null;
    }

    var passageText = string.Join(
      " ",
      passage.Verses.Select(v => v.Text));

    if (string.IsNullOrWhiteSpace(passageText))
    {
      return null;
    }

    var commentary = await _aiCommentaryService.GenerateCommentaryAsync(reading.Book.Name, reading.Chapter, reading.EndChapter, reading.StartVerse, reading.EndVerse, passageText);

    if (string.IsNullOrWhiteSpace(commentary))
    {
      return null;
    }

    reading.Commentary = commentary;
    await _context.SaveChangesAsync();

    return commentary;
  }
}
