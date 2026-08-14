namespace DailyDevotional.Api.DTOs.ESV;
public class ESVPassageResponse
{
  public string Query { get; set; } = string.Empty;

  public string Canonical { get; set; } = string.Empty;

  public List<List<long>> Parsed { get; set; } = [];

  public List<ESVPassageMeta> PassageMeta { get; set; } = [];

  public List<string> Passages { get; set; } = [];
}
