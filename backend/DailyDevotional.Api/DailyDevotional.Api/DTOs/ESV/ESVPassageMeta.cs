namespace DailyDevotional.Api.DTOs.ESV;
public class ESVPassageMeta
{
  public string Canonical { get; set; } = string.Empty;

  public List<long> ChapterStart { get; set; } = [];

  public List<long> ChapterEnd { get; set; } = [];

  public long PrevVerse { get; set; }

  public long NextVerse { get; set; }
}
