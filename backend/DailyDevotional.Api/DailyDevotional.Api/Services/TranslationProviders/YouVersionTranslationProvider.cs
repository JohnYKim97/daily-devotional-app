using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.Parsers;
using DailyDevotional.Api.Services.RateLimiting;

namespace DailyDevotional.Api.Services.TranslationProviders;

// Fetches licensed Bible text from the YouVersion Platform API (https://api.youversion.com/v1),
// which hands out publishers' translations (the NIV, the NASB, ...) under their own
// "Fast-track" license agreements. Translation.ExternalId is the YouVersion Bible id.
//
// Every call needs the app key (YouVersion:AppKey) in the X-YVP-App-Key header. Calls are
// counted against this app's own YouVersionApi allowance before they are made.
public class YouVersionTranslationProvider : ITranslationProvider
{
  private const int MaxRetries = 3;

  // Book id (1 Genesis ... 66 Revelation) to the USFM code YouVersion uses.
  private static readonly string[] UsfmCodes =
  [
    "GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH",
    "EZR", "NEH", "EST", "JOB", "PSA", "PRO", "ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS",
    "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL", "MAT", "MRK", "LUK",
    "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT",
    "PHM", "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD", "REV"
  ];

  private readonly HttpClient _httpClient;
  private readonly IProviderRateLimiter _rateLimiter;

  public TranslationProviderKind Kind => TranslationProviderKind.YouVersionApi;

  public YouVersionTranslationProvider(HttpClient httpClient, IConfiguration configuration, IProviderRateLimiter rateLimiter)
  {
    _httpClient = httpClient;
    _rateLimiter = rateLimiter;

    var appKey = configuration["YouVersion:AppKey"];

    if (!string.IsNullOrWhiteSpace(appKey))
    {
      _httpClient.DefaultRequestHeaders.Add("X-YVP-App-Key", appKey);
    }
  }

  public async Task<List<ProviderVerse>> GetVersesAsync(
    Translation translation,
    string bookName,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse,
    IReadOnlyDictionary<int, int> chapterVerseCounts)
  {
    var bibleId = translation.ExternalId
      ?? throw new InvalidOperationException($"{translation.Code} has no YouVersion Bible id.");

    var verses = new List<ProviderVerse>();

    // The API takes one chapter at a time (a range cannot cross a chapter boundary).
    for (var chapter = startChapter; chapter <= endChapter; chapter++)
    {
      var first = chapter == startChapter ? startVerse : 1;
      var last = chapter == endChapter ? endVerse : chapterVerseCounts[chapter];

      var reference = BuildReference(bookName, chapter, first, last, chapterVerseCounts[chapter]);

      var passage = await GetPassageAsync(bibleId, reference);

      if (passage?.Content is { Length: > 0 } html)
      {
        verses.AddRange(YouVersionPassageParser.Parse(html, chapter));
      }
    }

    return verses;
  }

  public async Task<string?> GetCopyrightNoticeAsync(Translation translation)
  {
    if (translation.ExternalId is not int bibleId)
    {
      return null;
    }

    var version = await GetAsync<YouVersionBible>($"bibles/{bibleId}");

    return string.IsNullOrWhiteSpace(version?.Copyright) ? null : version.Copyright.Trim();
  }

  private static string BuildReference(string bookName, int chapter, int first, int last, int chapterLength)
  {
    var usfm = UsfmCodes[BookIdOf(bookName) - 1];

    if (first == 1 && last >= chapterLength)
    {
      return $"{usfm}.{chapter}";
    }

    return first == last ? $"{usfm}.{chapter}.{first}" : $"{usfm}.{chapter}.{first}-{last}";
  }

  private static int BookIdOf(string bookName)
  {
    var book = Data.BibleBookData.Books.FirstOrDefault(b => b.Name == bookName)
      ?? throw new ArgumentException($"Unknown book '{bookName}'.");

    return book.Id;
  }

  private Task<YouVersionPassage?> GetPassageAsync(int bibleId, string reference) =>
    GetAsync<YouVersionPassage>(
      $"bibles/{bibleId}/passages/{reference}?format=html&include_headings=true&include_notes=true");

  private async Task<T?> GetAsync<T>(string url)
  {
    for (var attempt = 0; ; attempt++)
    {
      // Every HTTP call counts, including retries after a 429.
      _rateLimiter.EnsureAllowed(Kind);

      var response = await _httpClient.GetAsync(url);

      if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < MaxRetries)
      {
        var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));

        await Task.Delay(delay);
        continue;
      }

      response.EnsureSuccessStatusCode();

      return await response.Content.ReadFromJsonAsync<T>();
    }
  }

  private sealed class YouVersionPassage
  {
    [JsonPropertyName("content")]
    public string? Content { get; set; }
  }

  private sealed class YouVersionBible
  {
    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }
  }
}
