
namespace DailyDevotional.Api.Models;

public class ParsedReading
{
  public string Book { get; set; } = string.Empty;
  public int Chapter { get; set; }
  public int StartVerse { get; set; }
  public int EndVerse { get; set; }
  //True when the reading represents the entire chapter.
  public bool IsWholeChapter { get; set; }
  // True when the reading starts at a specific verse
  // and continues to the end of the chapter.
  public bool ContinuesToEndOfChapter { get; set; }
}
