using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DailyDevotional.Api.DTOs.ESV;
using DailyDevotional.Api.Models;
using System.Text.RegularExpressions;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

public class BibleService : IBibleService
{
  private readonly HttpClient _httpClient;

  public BibleService(HttpClient httpClient, IConfiguration configuration)
  {
    _httpClient = httpClient;

    var apiKey = configuration["ESV:ApiKey"];

    Console.WriteLine(
    $"ESV API key loaded: {!string.IsNullOrWhiteSpace(apiKey)}"
);

    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);
  }

  public async Task<List<DailyReadingVerse>> GetVersesAsync(
    string book,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    var reference = startChapter == endChapter
      ? $"{book} {startChapter}:{startVerse}-{endVerse}"
      : $"{book} {startChapter}:{startVerse}-{endChapter}:{endVerse}";

    var url = $"passage/text/?q={Uri.EscapeDataString(reference)}" +
            "&include-verse-numbers=true" +
            "&include-passage-references=false" +
            "&include-footnotes=false" +
            "&include-headings=false" +
            "&include-short-copyright=true";
    var response = await GetPassageResponseAsync(url);
    if (response == null || response.Passages.Count == 0)
    {
      return [];
    }

    var passage = response.Passages[0];

    Console.WriteLine("ESV Response");
    Console.WriteLine(passage);

    return ParseVerses(passage, startChapter);
  }

  private List<DailyReadingVerse> ParseVerses(string passage, int startChapter)
  {
    var verses = new List<DailyReadingVerse>();
    var matches = Regex.Matches(
       passage,
       @"\[(\d+)\]\s*(.*?)(?=\[\d+\]|$)",
       RegexOptions.Singleline
   );

    var currentChapter = startChapter;
    var previousVerseNumber = 0;

    foreach (Match match in matches)
    {
      var verseNumber = int.Parse(match.Groups[1].Value);
      var text = match.Groups[2].Value.Trim();

      if(verseNumber < previousVerseNumber)
      {
        currentChapter++;
      }

      previousVerseNumber = verseNumber;

      verses.Add(new DailyReadingVerse
      {
        Chapter = currentChapter,
        VerseNumber = verseNumber,
        Text = text
      });
    }

    return verses;
  }

  public async Task<int> GetChapterVerseCountAsync(string book, int chapter)
  {
    var reference = $"{book} {chapter}";

    var url =
        $"passage/text/?q={Uri.EscapeDataString(reference)}" +
        "&include-verse-numbers=true" +
        "&include-passage-references=false" +
        "&include-footnotes=false" +
        "&include-headings=false" +
        "&include-short-copyright=true";

    var response = await GetPassageResponseAsync(url);

    if (response == null || response.Passages.Count == 0)
    {
      throw new InvalidOperationException($"Could not retrieve {book} {chapter} from the ESV API.");
    }

    var passage = response.Passages[0];

    // We'll parse the final verse number from the ESV response.
    return ExtractLastVerseNumber(passage);
  }

  private static int ExtractLastVerseNumber(string passage)
  {
    var matches = Regex.Matches(passage, @"\[(\d+)\]");

    if (matches.Count == 0)
    {
      throw new InvalidOperationException("Could not determine the final verse number.");
    }

    return int.Parse(matches[^1].Groups[1].Value);
  }

  private const int MaxRetries = 3;

  private async Task<ESVPassageResponse?> GetPassageResponseAsync(string url)
  {
    for (var attempt = 0; ; attempt++)
    {
      var response = await _httpClient.GetAsync(url);

      if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < MaxRetries)
      {
        var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));

        await Task.Delay(delay);
        continue;
      }

      response.EnsureSuccessStatusCode();

      return await response.Content.ReadFromJsonAsync<ESVPassageResponse>();
    }
  }
}
