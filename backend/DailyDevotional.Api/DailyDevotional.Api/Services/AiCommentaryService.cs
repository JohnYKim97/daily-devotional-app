using Anthropic;
using Anthropic.Models.Messages;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

public class AiCommentaryService : IAiCommentaryService
{
  private readonly AnthropicClient? _client;

  public AiCommentaryService(IConfiguration configuration)
  {
    var apiKey = configuration["Anthropic:ApiKey"];

    _client = string.IsNullOrWhiteSpace(apiKey) ? null : new AnthropicClient { ApiKey = apiKey };
  }

  public async Task<string> GenerateCommentaryAsync(
    string book,
    int chapter,
    int endChapter,
    int startVerse,
    int endVerse,
    string passageText)
  {
    if (_client == null)
    {
      return string.Empty;
    }

    var reference = chapter == endChapter
      ? $"{book} {chapter}:{startVerse}-{endVerse}"
      : $"{book} {chapter}:{startVerse}-{endChapter}:{endVerse}";

    var prompt =
      $"Write a short devotional commentary (3-5 sentences) on {reference} for a Presbyterian " +
      "daily devotional reader. Focus on the passage's meaning and a practical application for " +
      "everyday faith. Do not repeat the verse text itself.\n\n" +
      $"Passage text:\n{passageText}";

    var response = await _client.Messages.Create(new MessageCreateParams
    {
      Model = "claude-opus-5",
      MaxTokens = 500,
      OutputConfig = new OutputConfig { Effort = Effort.Low },
      Messages = [new() { Role = Role.User, Content = prompt }],
    });

    return response.Content
      .Select(b => b.Value)
      .OfType<TextBlock>()
      .Select(b => b.Text)
      .FirstOrDefault()?.Trim() ?? string.Empty;
  }


}
