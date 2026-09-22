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

  public async Task<JournalResponse?> GetJournalByDateAsync(string userId, DateOnly date)
  {
    var journal = await _context.Journals.FirstOrDefaultAsync(j => j.UserId == userId && j.Date == date);

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

  public async Task<JournalResponse?> UpdateJournalAsync(string userId, DateOnly date, UpdateJournalRequest request)
  {
    var journal = await _context.Journals.FirstOrDefaultAsync(j => j.UserId == userId && j.Date == date);

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

  public async Task<List<JournalHistoryEntryResponse>> GetAllJournalsAsync(string currentUserId)
  {
    var journals = await _context.Journals
      .OrderByDescending(j => j.Date)
      .ToListAsync();

    var dates = journals.Select(j => j.Date).Distinct().ToList();
    var userIds = journals.Select(j => j.UserId).Distinct().ToList();

    var readingsByDate = await _context.DailyReadings
      .Where(r => dates.Contains(r.Date))
      .ToDictionaryAsync(r => r.Date);

    var usersById = await _context.Users
      .Where(u => userIds.Contains(u.Id))
      .ToDictionaryAsync(u => u.Id);

    var settingsByUserId = await _context.UserSettings
      .Where(s => userIds.Contains(s.UserId))
      .ToDictionaryAsync(s => s.UserId);

    var visibleJournals = journals.Where(journal =>
    {
      if (journal.UserId == currentUserId)
      {
        return true;
      }

      settingsByUserId.TryGetValue(journal.UserId, out var authorSettings);

      return authorSettings?.ShareHistory ?? true;
    });

    return visibleJournals.Select(journal =>
    {
      readingsByDate.TryGetValue(journal.Date, out var reading);
      usersById.TryGetValue(journal.UserId, out var author);
      settingsByUserId.TryGetValue(journal.UserId, out var authorSettings);

      var shareFavoriteVerse = authorSettings?.ShareFavoriteVerseInHistory ?? false;

      return new JournalHistoryEntryResponse
      {
        Id = journal.Id,
        Date = journal.Date,
        Book = reading?.Book ?? string.Empty,
        Chapter = reading?.Chapter ?? 0,
        EndChapter = reading?.EndChapter ?? 0,
        StartVerse = reading?.StartVerse ?? 0,
        EndVerse = reading?.EndVerse ?? 0,
        FavoriteVerse = shareFavoriteVerse ? journal.FavoriteVerse : null,
        Notes = journal.Notes,
        AuthorUserId = journal.UserId,
        AuthorName = FormatAuthorName(author),
        IsOwnEntry = journal.UserId == currentUserId,
      };
    }).ToList();
  }

  private static string FormatAuthorName(ApplicationUser? author)
  {
    if (author == null)
    {
      return "Unknown";
    }

    var fullName = $"{author.FirstName} {author.LastName}".Trim();

    return string.IsNullOrWhiteSpace(fullName) ? (author.Email ?? "Unknown") : fullName;
  }
}
