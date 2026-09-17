
namespace DailyDevotional.Api.Models;

public class ParsedReading
{
  public string Book { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int EndChapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  public bool IsWholeChapter { get; set; }
  public bool ContinuesToEndOfChapter { get; set; }
}
