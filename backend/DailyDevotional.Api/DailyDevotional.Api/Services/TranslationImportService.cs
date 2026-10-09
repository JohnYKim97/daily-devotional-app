using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using DailyDevotional.Api.Data;
using DailyDevotional.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Services;

// Loads translation structure (books, chapters, verse counts) for every translation
// and the full verse text for translations that are allowed to be stored in full.
public partial class TranslationImportService
{
  private const string StructureResourceName = "DailyDevotional.Api.Data.Seed.translation-structure.json";

  // An import is refused when less of the expected text than this arrives, so a truncated
  // or wrong download is never enabled.
  private const double MinimumCoverage = 0.99;

  private readonly AppDbContext _context;
  private readonly HttpClient _httpClient;
  private readonly ILogger<TranslationImportService> _logger;

  public TranslationImportService(AppDbContext context, HttpClient httpClient, ILogger<TranslationImportService> logger)
  {
    _context = context;
    _httpClient = httpClient;
    _logger = logger;
  }

  // Verse counts are facts rather than copyrighted text, so they ship with the app. They are
  // loaded for every translation, and replaced when a translation's stored counts differ
  // from the shipped ones (e.g. after a correction in a new version of the app).
  public async Task SeedStructureAsync()
  {
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(StructureResourceName)
      ?? throw new InvalidOperationException($"Embedded resource '{StructureResourceName}' was not found.");

    var structure = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, int[]>>>(stream)
      ?? throw new InvalidOperationException("translation-structure.json is empty.");

    var translations = await _context.Translations.ToListAsync();

    foreach (var translation in translations)
    {
      if (!structure.TryGetValue(translation.Code, out var books))
      {
        continue;
      }

      var expectedChapters = books.Values.Sum(counts => counts.Length);
      var expectedVerses = books.Values.Sum(counts => counts.Sum());

      var storedChapters = await _context.TranslationChapters.CountAsync(tc => tc.TranslationId == translation.Id);
      var storedVerses = await _context.TranslationChapters
        .Where(tc => tc.TranslationId == translation.Id)
        .SumAsync(tc => (int?)tc.VerseCount) ?? 0;

      if (storedChapters == expectedChapters && storedVerses == expectedVerses)
      {
        continue;
      }

      await _context.TranslationChapters.Where(tc => tc.TranslationId == translation.Id).ExecuteDeleteAsync();
      await _context.TranslationBooks.Where(tb => tb.TranslationId == translation.Id).ExecuteDeleteAsync();
      _context.ChangeTracker.Clear();

      foreach (var (bookKey, verseCounts) in books)
      {
        var bookId = int.Parse(bookKey);

        _context.TranslationBooks.Add(new TranslationBook
        {
          TranslationId = translation.Id,
          BookId = bookId,
          ChapterCount = verseCounts.Length
        });

        for (var i = 0; i < verseCounts.Length; i++)
        {
          _context.TranslationChapters.Add(new TranslationChapter
          {
            TranslationId = translation.Id,
            BookId = bookId,
            Chapter = i + 1,
            VerseCount = verseCounts[i]
          });
        }
      }

      await _context.SaveChangesAsync();
      _logger.LogInformation("Seeded chapter structure for {Code}.", translation.Code);
    }
  }

  // Imports the whole text of a Full-mode translation from a bolls.life JSON file
  // (downloaded from bolls.life unless a local file is given), then enables it.
  //
  // Only verses that exist in the translation's stored structure are kept, which drops
  // anything extra in the source (e.g. the apocryphal Additions to Esther that the bolls.life
  // KJV file carries in the book of Esther). The import is one transaction and nothing is
  // changed when the source is incomplete.
  public async Task<int> ImportFullTranslationAsync(string code, string? filePath)
  {
    var translation = await _context.Translations.FirstOrDefaultAsync(t => t.Code == code)
      ?? throw new InvalidOperationException($"Unknown translation '{code}'.");

    if (translation.StorageMode != TranslationStorageMode.Full)
    {
      throw new InvalidOperationException(
        $"{code} is license-restricted and cannot be stored in full. It is fetched from its provider and cached instead.");
    }

    var chapterCounts = await _context.TranslationChapters
      .AsNoTracking()
      .Where(tc => tc.TranslationId == translation.Id)
      .ToDictionaryAsync(tc => (tc.BookId, tc.Chapter), tc => tc.VerseCount);

    if (chapterCounts.Count == 0)
    {
      throw new InvalidOperationException($"The chapter structure of {code} has not been loaded.");
    }

    var json = filePath != null
      ? await File.ReadAllTextAsync(filePath)
      : await _httpClient.GetStringAsync($"https://bolls.life/static/translations/{code}.json");

    var source = JsonSerializer.Deserialize<List<BollsVerse>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
      ?? throw new InvalidOperationException("The translation file is empty.");

    var verses = source
      .Where(v => chapterCounts.TryGetValue((v.Book, v.Chapter), out var count) && v.Verse >= 1 && v.Verse <= count)
      .DistinctBy(v => (v.Book, v.Chapter, v.Verse))
      .Select(v => new Verse
      {
        TranslationId = translation.Id,
        BookId = v.Book,
        Chapter = v.Chapter,
        VerseNumber = v.Verse,
        Text = CleanText(v.Text)
      })
      .Where(v => v.Text.Length > 0)
      .ToList();

    var expected = chapterCounts.Values.Sum();

    if (verses.Count < expected * MinimumCoverage)
    {
      throw new InvalidOperationException(
        $"The {code} file has {verses.Count} of the expected {expected} verses; nothing was imported.");
    }

    await using var transaction = await _context.Database.BeginTransactionAsync();

    await _context.Verses.Where(v => v.TranslationId == translation.Id).ExecuteDeleteAsync();

    const int batchSize = 5000;

    for (var i = 0; i < verses.Count; i += batchSize)
    {
      _context.Verses.AddRange(verses.Skip(i).Take(batchSize));
      await _context.SaveChangesAsync();
      _context.ChangeTracker.Clear();
    }

    await _context.Translations
      .Where(t => t.Id == translation.Id)
      .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsEnabled, true));

    await transaction.CommitAsync();

    _logger.LogInformation("Imported {Count} verses of {Code} and enabled it.", verses.Count, code);

    return verses.Count;
  }

  // bolls.life text contains Strong's number tags (<S>), footnotes (<sup>) and
  // occasional <i>/<b> markup; keep only the plain verse text.
  private static string CleanText(string html)
  {
    var text = StrongsTagRegex().Replace(html, string.Empty);
    text = FootnoteRegex().Replace(text, string.Empty);
    text = AnyTagRegex().Replace(text, string.Empty);
    text = WebUtility.HtmlDecode(text);
    return WhitespaceRegex().Replace(text, " ").Trim();
  }

  [GeneratedRegex(@"<S>.*?</S>", RegexOptions.Singleline)]
  private static partial Regex StrongsTagRegex();

  [GeneratedRegex(@"<sup>.*?</sup>", RegexOptions.Singleline)]
  private static partial Regex FootnoteRegex();

  [GeneratedRegex(@"<[^>]+>")]
  private static partial Regex AnyTagRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceRegex();

  private record BollsVerse(int Book, int Chapter, int Verse, string Text);
}
