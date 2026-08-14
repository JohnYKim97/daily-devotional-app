using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DailyDevotional.Api.DTOs.ESV;
using DailyDevotional.Api.Models;
using System.Text.RegularExpressions;

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
    int chapter,
    int startVerse,
    int endVerse)
  {
    var reference = $"{book} {chapter}:{startVerse}-{endVerse}";
    var url = $"passage/text/?q={Uri.EscapeDataString(reference)}" +
            "&include-verse-numbers=true" +
            "&include-passage-references=false" +
            "&include-footnotes=false" +
            "&include-headings=false" +
            "&include-short-copyright=true";
    var response = await _httpClient.GetFromJsonAsync<ESVPassageResponse>(url);
    if (response == null || response.Passages.Count == 0)
    {
      return [];
    }

    var passage = response.Passages[0];

    Console.WriteLine("ESV Response");
    Console.WriteLine(passage);

    return ParseVerses(passage);
  }

  private List<DailyReadingVerse> ParseVerses(string passage)
  {
    var verses = new List<DailyReadingVerse>();
    var matches = Regex.Matches(
       passage,
       @"\[(\d+)\]\s*(.*?)(?=\[\d+\]|$)",
       RegexOptions.Singleline
   );

    foreach (Match match in matches)
    {
      var verseNumber = int.Parse(match.Groups[1].Value);
      var text = match.Groups[2].Value.Trim();

      verses.Add(new DailyReadingVerse
      {
        VerseNumber = verseNumber,
        Text = text
      });
    }

    return verses;
  }
}
