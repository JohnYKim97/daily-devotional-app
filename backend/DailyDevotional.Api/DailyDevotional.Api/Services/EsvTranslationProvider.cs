using System.Net.Http.Headers;
using System.Net.Http.Json;
using DailyDevotional.Api.DTOs.ESV;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.RateLimiting;

namespace DailyDevotional.Api.Services;

public class EsvTranslationProvider : ITranslationProvider
{
  private const int MaxRetries = 3;

  private readonly HttpClient _httpClient;
  private readonly IProviderRateLimiter _rateLimiter;

  public TranslationProviderKind Kind => TranslationProviderKind.EsvApi;

  public EsvTranslationProvider(HttpClient httpClient, IConfiguration configuration, IProviderRateLimiter rateLimiter)
  {
    _httpClient = httpClient;
    _rateLimiter = rateLimiter;

    var apiKey = configuration["ESV:ApiKey"];

    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);
  }

  public async Task<List<ProviderVerse>> GetVersesAsync(
    string bookName,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse)
  {
    var reference = startChapter == endChapter
      ? $"{bookName} {startChapter}:{startVerse}-{endVerse}"
      : $"{bookName} {startChapter}:{startVerse}-{endChapter}:{endVerse}";

    var url = $"passage/text/?q={Uri.EscapeDataString(reference)}" +
            "&include-verse-numbers=true" +
            "&include-passage-references=false" +
            "&include-footnotes=true" +
            "&include-footnote-body=true" +
            "&include-headings=true" +
            "&include-short-copyright=true";

    var response = await GetPassageResponseAsync(url);

    if (response == null || response.Passages.Count == 0)
    {
      return [];
    }

    return EsvPassageParser.Parse(response.Passages[0], startChapter);
  }

  private async Task<ESVPassageResponse?> GetPassageResponseAsync(string url)
  {
    for (var attempt = 0; ; attempt++)
    {
      // Every HTTP call counts, including retries after a 429 from the ESV.
      _rateLimiter.EnsureAllowed(Kind);

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
