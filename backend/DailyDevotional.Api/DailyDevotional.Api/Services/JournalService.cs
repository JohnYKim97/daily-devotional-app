using DailyDevotional.Api.Data;
using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

public class JournalService : IJournalService
{
  private readonly AppDbContext _context;

  public JournalService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<JournalResponse> CreateJournalAsync(CreateJournalRequest request)
  {
    var journal = new Journal
    {
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

  public async Task<JournalResponse?> GetJournalByDateAsync(DateTime date)
  {
    var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Date.Date == date.Date);

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
}
