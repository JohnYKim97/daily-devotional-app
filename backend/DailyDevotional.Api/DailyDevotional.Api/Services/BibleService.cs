using System.Net.Http.Json;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.DTOs;


namespace DailyDevotional.Api.Services;

public class BibleService : IBibleService
{
  private readonly HttpClient _httpClient;

  public BibleService(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<List<DailyReadingVerse>> GetVersesAsync(
    string book,
    int chapter,
    int startVerse,
    int endVerse)
  {
    var reference = $"{book} {chapter}:{startVerse}-{endVerse}";
    var url = $"/api/{Uri.EscapeDataString(reference)}?translation=kjv";
    var response = await _httpClient.GetFromJsonAsync<BibleApiResponse>(url);

    if (response == null)
    {
      return new List<DailyReadingVerse>();
    }

    return response.Verses
      .Select(v => new DailyReadingVerse
      {
        VerseNumber = v.Verse,
        Text = v.Text
      })
      .ToList();
      
  }

}
