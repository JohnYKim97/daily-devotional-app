using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

public class DailyReadingImportService : IDailyReadingImportService
{
  private readonly IBibleService _bibleService;

  public DailyReadingImportService(IBibleService bibleService)
  {
    _bibleService = bibleService;
  }
  public List<ParsedReading> ParseDocument(string filePath)
  {
    using var document = WordprocessingDocument.Open(
        filePath,
        false);

    var body = document.MainDocumentPart!
        .Document
        .Body!;

    var tables = body
        .Elements<Table>()
        .ToList();

    var readings = new List<ParsedReading>();

    var firstParagraph = body
      .Elements<Paragraph>()
      .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.InnerText));

    var currentBook = string.Empty;
    var currentChapter = 0;

    if (firstParagraph != null)
    {
      currentBook = GetBookName(firstParagraph.InnerText)
        ?? throw new InvalidOperationException(
          $"Could not determine the Bible book from " +
          $"document heading '{firstParagraph.InnerText}'.");
    }

    foreach (var table in tables)
    {
      ParseTable(table, readings, ref currentBook, ref currentChapter);
    }

    return readings;
  }

  private static void ParseTable(
      Table table,
      List<ParsedReading> readings,
      ref string currentBook,
      ref int currentChapter)
  {
    var rows = table
        .Elements<TableRow>()
        .ToList();

    if (rows.Count == 0)
    {
      return;
    }

    var columnCount = rows
        .Max(row => row.Elements<TableCell>().Count());

    // IMPORTANT:
    // The schedule is organized vertically by column.
    //
    // Column 1:
    //   row 1
    //   row 2
    //   row 3
    //   ...
    //
    // Then column 2, etc.
    for (var columnIndex = 0;
         columnIndex < columnCount;
         columnIndex++)
    {
      foreach (var row in rows)
      {
        var cells = row
            .Elements<TableCell>()
            .ToList();

        if (columnIndex >= cells.Count)
        {
          continue;
        }

        var text = cells[columnIndex]
            .InnerText
            .Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
          continue;
        }

        text = NormalizeText(text);

        // Check whether this cell changes the book.
        var bookName = GetBookName(text);

        if (bookName != null)
        {
          currentBook = bookName;
          currentChapter = 0;
          continue;
        }

        var reading = ParseReading(
            text,
            currentBook,
            currentChapter);

        if (reading == null)
        {
          continue;
        }

        currentChapter = reading.Chapter;

        readings.Add(reading);
      }
    }
  }

  private static ParsedReading? ParseReading(
      string text,
      string currentBook,
      int currentChapter)
  {
    if (string.IsNullOrWhiteSpace(currentBook))
    {
      return null;
    }

    /*
     * FORMAT 1
     *
     * 1:1-20
     * 51:1-16
     */
    var chapterAndRange = Regex.Match(
        text,
        @"^(?<chapter>\d+):(?<start>\d+)-(?<end>\d+)$");

    if (chapterAndRange.Success)
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = int.Parse(
              chapterAndRange.Groups["chapter"].Value),
        EndChapter = int.Parse(chapterAndRange.Groups["chapter"].Value),
        StartVerse = int.Parse(
              chapterAndRange.Groups["start"].Value),
        EndVerse = int.Parse(
              chapterAndRange.Groups["end"].Value)
      };
    }

    /*
     * FORMAT 2
     *
     * 46:1-
     *
     * Starts at a specific verse and
     * continues to the end of the chapter.
     */
    var chapterToEnd = Regex.Match(
        text,
        @"^(?<chapter>\d+):(?<start>\d+)-$");

    if (chapterToEnd.Success)
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = int.Parse(
              chapterToEnd.Groups["chapter"].Value),
        EndChapter = int.Parse(chapterToEnd.Groups["chapter"].Value),
        StartVerse = int.Parse(
              chapterToEnd.Groups["start"].Value),
        EndVerse = 0,
        ContinuesToEndOfChapter = true
      };
    }

    /*
     * FORMAT 3
     *
     * 12:
     * 39:
     * 60:
     *
     * Entire chapter.
     */
    var chapterOnlyWithColon = Regex.Match(
        text,
        @"^(?<chapter>\d+):$");

    if (chapterOnlyWithColon.Success)
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = int.Parse(
              chapterOnlyWithColon.Groups["chapter"].Value),
        EndChapter = int.Parse(chapterOnlyWithColon.Groups["chapter"].Value),
        StartVerse = 1,
        EndVerse = 0,
        IsWholeChapter = true
      };
    }

    /*
     * FORMAT 4
     *
     * 21-31
     * 17-23
     * 8-30
     *
     * Uses the current chapter.
     */
    var verseRange = Regex.Match(
        text,
        @"^(?<start>\d+)-(?<end>\d+)$");

    if (verseRange.Success && currentChapter > 0)
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = currentChapter,
        EndChapter = currentChapter,
        StartVerse = int.Parse(
              verseRange.Groups["start"].Value),
        EndVerse = int.Parse(
              verseRange.Groups["end"].Value)
      };
    }

    /*
     * FORMAT 4b
     *
     * 23-3:6
     * 31-9:1
     *
     * Starts in the current chapter and continues into
     * a later chapter, ending at a specific verse there.
     */
    var crossChapterRange = Regex.Match(
        text,
        @"^(?<start>\d+)-(?<endChapter>\d+):(?<endVerse>\d+)$");

    if (crossChapterRange.Success && currentChapter > 0)
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = currentChapter,
        EndChapter = int.Parse(crossChapterRange.Groups["endChapter"].Value),
        StartVerse = int.Parse(crossChapterRange.Groups["start"].Value),
        EndVerse = int.Parse(crossChapterRange.Groups["endVerse"].Value)
      };
    }

    /*
     * FORMAT 5
     *
     * 1:25
     *
     * A single verse.
     */
    var chapterAndVerse = Regex.Match(
        text,
        @"^(?<chapter>\d+):(?<verse>\d+)$");

    if (chapterAndVerse.Success)
    {
      var chapter = int.Parse(
          chapterAndVerse.Groups["chapter"].Value);

      var verse = int.Parse(
          chapterAndVerse.Groups["verse"].Value);

      return new ParsedReading
      {
        Book = currentBook,
        Chapter = chapter,
        EndChapter = chapter,
        StartVerse = verse,
        EndVerse = verse
      };
    }

    /*
     * FORMAT 6
     *
     * 4
     * 16
     * 25
     * 52
     *
     * Entire chapter.
     */
    if (int.TryParse(text, out var wholeChapter))
    {
      return new ParsedReading
      {
        Book = currentBook,
        Chapter = wholeChapter,
        EndChapter = wholeChapter,
        StartVerse = 1,
        EndVerse = 0,
        IsWholeChapter = true
      };
    }

    return null;
  }

  private static string NormalizeText(string text)
  {
    text = text
        .Replace("`", "")
        .Trim();

    /*
     * The document contains:
     *
     * 50:1:16
     *
     * Treat it as:
     *
     * 50:1-16
     */
    text = Regex.Replace(
        text,
        @"^(\d+):(\d+):(\d+)$",
        "$1:$2-$3");

    return text;
  }

  private static bool IsBookName(string text)
  {
    return BibleBooks.Contains(text);
  }

  private static readonly HashSet<string> BibleBooks =
      new(StringComparer.OrdinalIgnoreCase)
      {
            "Genesis",
            "Exodus",
            "Leviticus",
            "Numbers",
            "Deuteronomy",
            "Joshua",
            "Judges",
            "Ruth",

            "1 Samuel",
            "2 Samuel",
            "1 Kings",
            "2 Kings",
            "1 Chronicles",
            "2 Chronicles",

            "Ezra",
            "Nehemiah",
            "Esther",
            "Job",
            "Psalms",
            "Proverbs",
            "Ecclesiastes",
            "Song of Solomon",

            "Isaiah",
            "Jeremiah",
            "Lamentations",
            "Ezekiel",
            "Daniel",
            "Hosea",
            "Joel",
            "Amos",
            "Obadiah",
            "Jonah",
            "Micah",
            "Nahum",
            "Habakkuk",
            "Zephaniah",
            "Haggai",
            "Zechariah",
            "Malachi",

            "Matthew",
            "Mark",
            "Luke",
            "John",
            "Acts",
            "Romans",

            "1 Corinthians",
            "2 Corinthians",
            "Galatians",
            "Ephesians",
            "Philippians",
            "Colossians",

            "1 Thessalonians",
            "2 Thessalonians",
            "1 Timothy",
            "2 Timothy",
            "Titus",
            "Philemon",

            "Hebrews",
            "James",
            "1 Peter",
            "2 Peter",
            "1 John",
            "2 John",
            "3 John",
            "Jude",
            "Revelation"
      };

  public List<DailyReading> CreateDailyReadings(List<ParsedReading> parsedReadings, DateOnly startDate)
  {
    var readings = new List<DailyReading>();

    var currentDate = startDate;

    foreach (var parsed in parsedReadings)
    {
      readings.Add(new DailyReading
      {
        Date = currentDate,
        Book = parsed.Book,
        Chapter = parsed.Chapter,
        EndChapter = parsed.EndChapter,
        StartVerse = parsed.StartVerse,
        EndVerse = parsed.EndVerse,
      });

      currentDate = currentDate.AddDays(1);
    }

    return readings;
  }

  private static string? GetBookName(string text)
  {
    text = text.Trim();

    if (BibleBooks.Contains(text))
    {
      return BibleBooks.First(
          book => book.Equals(
              text,
              StringComparison.OrdinalIgnoreCase));
    }

    return null;
  }

  public List<string> ValidateReadings(List<ParsedReading> readings)
  {
    var errors = new List<string>();

    for (var i = 0; i < readings.Count; i++)
    {
      var reading = readings[i];

      if (string.IsNullOrWhiteSpace(reading.Book))
      {
        errors.Add($"Reading {i + 1}: missing book.");
      }

      if (reading.Chapter <= 0)
      {
        errors.Add($"Reading {i + 1}: invalid chapter.");
      }

      if (reading.StartVerse <= 0)
      {
        errors.Add($"Reading {i + 1}: invalid start verse.");
      }

      if (reading.EndChapter < reading.Chapter)
      {
        errors.Add($"Reading {i + 1}: end chapter is before start chapter.");
      }
      else if (
          reading.EndChapter == reading.Chapter &&
          !reading.IsWholeChapter &&
          !reading.ContinuesToEndOfChapter &&
          reading.EndVerse < reading.StartVerse)
      {
        errors.Add($"Reading {i + 1}: end verse is before start verse.");
      }
    }

    return errors;
  }

  public async Task ResolveVerseRangesAsync(List<ParsedReading> readings)
  {
    var chapterVerseCounts = new Dictionary<(string Book, int Chapter), int>();

    foreach (var reading in readings)
    {
      if (!reading.IsWholeChapter && !reading.ContinuesToEndOfChapter)
      {
        continue;
      }

      var key = (reading.Book, reading.Chapter);

      if (!chapterVerseCounts.TryGetValue(key, out var endVerse))
      {
        endVerse = await _bibleService.GetChapterVerseCountAsync(reading.Book, reading.Chapter);
        chapterVerseCounts[key] = endVerse;

        // Avoid tripping the ESV API's rate limit when a schedule has
        // many whole-chapter readings to resolve in one import.
        await Task.Delay(200);
      }

      reading.EndVerse = endVerse;
    }
  }
}
