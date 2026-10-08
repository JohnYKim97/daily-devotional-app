namespace DailyDevotional.Api.DTOs;

public class TranslationResponse
{
  public int Id { get; set; }
  public string Code { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Language { get; set; } = string.Empty;
  public string CopyrightNotice { get; set; } = string.Empty;
}
