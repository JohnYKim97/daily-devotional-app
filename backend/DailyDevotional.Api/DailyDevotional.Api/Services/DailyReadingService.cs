using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;

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

  public async Task<DailyReadingResponse> GetReadingByDateAsync(DateOnly date)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Verses)
      .FirstOrDefaultAsync(r => r.Date == date);

    if (reading == null)
    {
      return null;
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

  public async Task<bool> ImportVersesAsync(int readingId)
  {
    var reading = await _context.DailyReadings
      .Include(r => r.Verses)
      .FirstOrDefaultAsync(r => r.Id == readingId);

    if (reading == null)
    {
      return false;
    }

    var verses = await _bibleService.GetVersesAsync(
      reading.Book,
      reading.Chapter,
      reading.StartVerse,
      reading.EndVerse);

    if (verses.Count == 0)
    {
      return false;
    }

    // Remove existing verses first
    _context.DailyReadingVerses.RemoveRange(reading.Verses);

    // Attach the newly imported verses
    foreach (var verse in verses)
    {
      verse.DailyReadingId = reading.Id;
    }

    await _context.DailyReadingVerses.AddRangeAsync(verses);

    await _context.SaveChangesAsync();

    return true;
  }
}
