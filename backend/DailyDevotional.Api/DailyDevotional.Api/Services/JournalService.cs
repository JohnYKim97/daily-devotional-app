using DailyDevotional.Api.Data;
using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace DailyDevotional.Api.Services;

public class JournalService : IJournalService
{
  private readonly AppDbContext _context;

  public JournalService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<JournalResponse> CreateJournalAsync(string userId, CreateJournalRequest request)
  {
    var journal = new Journal
    {
      UserId = userId,
      Date = request.Date,
      PassageReference = request.PassageReference,
      FavoriteVerse = request.FavoriteVerse,
      Notes = request.Notes,
    };

    _context.Journals.Add(journal);

    await _context.SaveChangesAsync();

    return new JournalResponse
    {
      Id = journal.Id,
      Date = journal.Date,
      PassageReference = journal.PassageReference,
      FavoriteVerse = journal.FavoriteVerse,
      Notes = journal.Notes,
    };
  }

  public async Task<JournalResponse?> GetJournalByDateAsync(DateOnly date)
  {
    var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Date == date);

    if (journal == null)
    {
      return null;
    }

    return new JournalResponse
    {
      Id = journal.Id,
      Date = journal.Date,
      PassageReference = journal.PassageReference,
      FavoriteVerse = journal?.FavoriteVerse,
      Notes = journal.Notes
    };
  }

  public async Task<JournalResponse> UpdateJournalAsync(DateOnly date, UpdateJournalRequest request)
  {
    var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Date == date);

    if (journal == null) {
      return null;
    }

    journal.PassageReference = request.PassageReference;
    journal.FavoriteVerse = request.FavoriteVerse;
    journal.Notes = request.Notes;

    await _context.SaveChangesAsync();

    return new JournalResponse
    {
      Id = journal.Id,
      Date = journal.Date,
      PassageReference = journal.PassageReference,
      FavoriteVerse = journal.FavoriteVerse,
      Notes = journal.Notes
    };
  }

  public async Task<List<JournalHistoryEntryResponse>> GetAllJournalsAsync()
  {
    var journals = await _context.Journals
      .OrderBy(j => j.Date)
      .ToListAsync();

    var dates = journals.Select(j => j.Date).ToList();

    var readingsByDate = await _context.DailyReadings
      .Where(r => dates.Contains(r.Date))
      .ToDictionaryAsync(r => r.Date);

    return journals.Select(journal =>
    {
      readingsByDate.TryGetValue(journal.Date, out var reading);

      return new JournalHistoryEntryResponse
      {
        Id = journal.Id,
        Date = journal.Date,
        Book = reading?.Book ?? string.Empty,
        Chapter = reading?.Chapter ?? 0,
        StartVerse = reading?.StartVerse ?? 0,
        EndVerse = reading?.EndVerse ?? 0,
        Notes = journal.Notes,
      };
    }).ToList();
  }
}
