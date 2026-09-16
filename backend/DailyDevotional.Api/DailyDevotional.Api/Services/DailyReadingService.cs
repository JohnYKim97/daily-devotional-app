using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

public class DailyReadingService : IDailyReadingService
{
  public readonly AppDbContext _context;
  private readonly IBibleService _bibleService;

  public DailyReadingService(AppDbContext context, IBibleService bibleService)
  {
    _context = context;
    _bibleService = bibleService;
  }

  public async Task<DailyReadingResponse?> GetReadingByDateAsync(DateOnly date)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Verses)
      .FirstOrDefaultAsync(r => r.Date == date);

    if (reading == null)
    {
      return null;
    }

    if (reading.Verses.Count == 0)
    {
      try
      {
        await FetchAndAttachVersesAsync(reading);
      }
      catch (HttpRequestException)
      {
        // Verse text couldn't be fetched right now (e.g. the ESV API is
        // rate-limited or briefly unavailable). The rest of the reading
        // still loads; this will simply retry on the next request for
        // this date.
      }
    }

    return new DailyReadingResponse
    {
      Id = reading.Id,
      Date = reading.Date,
      Book = reading.Book,
      Chapter = reading.Chapter,
      StartVerse = reading.StartVerse,
      EndVerse = reading.EndVerse,
      Commentary = reading.Commentary,
      Verses = reading.Verses
      .OrderBy(v => v.VerseNumber)
      .Select(v => new DailyReadingVerseResponse
      {
        Number = v.VerseNumber,
        Text = v.Text
      })
      .ToList()
    };
  }

  public async Task<List<DailyReadingSummaryResponse>> GetScheduleAsync(string userId)
  {
    var readings = await _context.DailyReadings
      .OrderBy(r => r.Date)
      .ToListAsync();

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
        Book = r.Book,
        Chapter = r.Chapter,
        StartVerse = r.StartVerse,
        EndVerse = r.EndVerse,
        HasNotes = dateswithNotesSet.Contains(r.Date),
      })
      .ToListAsync();
  }

  public async Task<bool> ImportVersesAsync(int readingId)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Verses)
      .FirstOrDefaultAsync(r => r.Id == readingId);

    if (reading == null)
    {
      return false;
    }

    _context.DailyReadingVerses.RemoveRange(reading.Verses);
    reading.Verses.Clear();

    return await FetchAndAttachVersesAsync(reading);
  }

  private async Task<bool> FetchAndAttachVersesAsync(DailyReading reading)
  {
    var verses = await _bibleService.GetVersesAsync(
      reading.Book,
      reading.Chapter,
      reading.StartVerse,
      reading.EndVerse);

    if (verses.Count == 0)
    {
      return false;
    }

    foreach (var verse in verses)
    {
      verse.DailyReadingId = reading.Id;
    }

    await _context.DailyReadingVerses.AddRangeAsync(verses);
    await _context.SaveChangesAsync();

    reading.Verses = verses;

    return true;
  }

  public async Task<ImportReadingsResponse> SaveImportedReadingsAsync(
      List<DailyReading> readings,
      bool overwrite)
  {
    var dates = readings
      .Select(r => r.Date)
      .ToList();

    var existingReadings = await _context.DailyReadings
      .Include(r => r.Verses)
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
      _context.DailyReadingVerses.RemoveRange(
          existingReadings.SelectMany(r => r.Verses));

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
}
