using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.Parsers;
using DailyDevotional.Api.Services.RateLimiting;

namespace DailyDevotional.Api.Services.TranslationProviders;

// Fetches New Living Translation text from the NLT API (https://api.nlt.to).
//
// Without a key the API treats the caller as anonymous: no more than 50 verses per request
// and 500 requests per day, non-commercial use only. A personal key (NLT:ApiKey, from
// https://api.nlt.to/Account/Register) raises that to 500 verses per request and 5,000
// requests per day. Requests are split to stay within the verse limit that applies.
public class NltTranslationProvider : ITranslationProvider
{
  private const string AnonymousKey = "TEST";
  private const int AnonymousVersesPerRequest = 50;
  private const int KeyedVersesPerRequest = 500;
  private const int MaxRetries = 3;

  private readonly HttpClient _httpClient;
  private readonly IProviderRateLimiter _rateLimiter;
  private readonly string _apiKey;

  public TranslationProviderKind Kind => TranslationProviderKind.NltApi;

  public NltTranslationProvider(HttpClient httpClient, IConfiguration configuration, IProviderRateLimiter rateLimiter)
  {
    _httpClient = httpClient;
    _rateLimiter = rateLimiter;

    var apiKey = configuration["NLT:ApiKey"];

    _apiKey = string.IsNullOrWhiteSpace(apiKey) ? AnonymousKey : apiKey;
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
    var verses = new List<ProviderVerse>();

    foreach (var reference in BuildRequests(bookName, startChapter, startVerse, endChapter, endVerse, chapterVerseCounts))
    {
      var html = await GetPassageHtmlAsync(reference);

      verses.AddRange(NltPassageParser.Parse(html));
    }

    return verses;
  }

  // References like "Psalms.119.1-50;Psalms.119.51-100", grouped so that no request asks for
  // more verses than the API allows.
  private List<string> BuildRequests(
    string bookName,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse,
    IReadOnlyDictionary<int, int> chapterVerseCounts)
  {
    var limit = _apiKey == AnonymousKey ? AnonymousVersesPerRequest : KeyedVersesPerRequest;

    var requests = new List<string>();
    var parts = new List<string>();
    var versesInRequest = 0;

    for (var chapter = startChapter; chapter <= endChapter; chapter++)
    {
      var first = chapter == startChapter ? startVerse : 1;
      var last = chapter == endChapter ? endVerse : chapterVerseCounts[chapter];

      for (var from = first; from <= last;)
      {
        if (versesInRequest == limit)
        {
          requests.Add(string.Join(';', parts));
          parts.Clear();
          versesInRequest = 0;
        }

        var to = Math.Min(last, from + (limit - versesInRequest) - 1);

        parts.Add($"{bookName}.{chapter}.{from}-{to}");
        versesInRequest += to - from + 1;
        from = to + 1;
      }
    }

    if (parts.Count > 0)
    {
      requests.Add(string.Join(';', parts));
    }

    return requests;
  }

  private async Task<string> GetPassageHtmlAsync(string reference)
  {
    var url = $"passages?ref={Uri.EscapeDataString(reference)}&version=NLT&key={Uri.EscapeDataString(_apiKey)}";

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

      return await response.Content.ReadAsStringAsync();
    }
  }
}
