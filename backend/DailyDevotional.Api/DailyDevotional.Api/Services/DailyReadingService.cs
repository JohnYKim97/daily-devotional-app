using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

public class DailyReadingService : IDailyReadingService
{
  public readonly AppDbContext _context;

  public DailyReadingService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<DailyReadingResponse> GetReadingByDateAsync(DateOnly date)
  {
    var reading = _context.DailyReadings
      .Include(r => r.Verses)
      .FirstOrDefault(r => r.Date == date);

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
}
