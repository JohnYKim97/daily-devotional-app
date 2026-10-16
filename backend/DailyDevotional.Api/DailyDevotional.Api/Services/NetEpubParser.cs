using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DailyDevotional.Api.Data;

namespace DailyDevotional.Api.Services;

public record ParsedVerse(
  int BookId,
  int Chapter,
  int VerseNumber,
  string Text,
  string? Heading,
  List<string> Footnotes);

// Reads the free NET Bible 2.1 EPUB from bible.org (https://bible.org/downloads).
//
// The EPUB has one XHTML file per chapter (fileN.xhtml) and one for that chapter's notes
// (fileN_notes.xhtml). Verses start at <span class="verse">c:v</span>, note markers are
// <sup><a id="n...">, and section headings, Psalm titles and speaker labels are paragraphs
// with their own classes.
//
// What is kept: the verse text, section headings (Psalm titles and superscriptions,
// Song of Songs speakers and the Hebrew letters of Lamentations 3 included) and every
// translator's note (tn), study note (sn) and text-critical note (tc), when notes are
// switched on. The cover, preface,
// book indexes, end matter, fonts and images are skipped.
//
// Output follows the format the verse list already shows for the ESV: the verse text has
// [[n]] markers, n being the position in the footnote list, and *asterisks* make italics
// in a note. Poetry lines are joined into one paragraph per verse. A note on a heading
// (e.g. on "Psalm 2") is shown from the start of the verse the heading sits above.
public static partial class NetEpubParser
{
  // Paragraph classes that hold a heading line rather than verse text.
  private static readonly HashSet<string> HeadingClasses =
    ["paragraphtitle", "psasuper", "sosspeaker", "lamhebrew"];

  private static readonly Dictionary<string, string> NoteLabels = new()
  {
    ["tn"] = "TN",
    ["sn"] = "SN",
    ["tc"] = "TC"
  };

  // The EPUB says "Song of Solomon"; the app's book list says "Song of Songs".
  private static readonly Dictionary<string, int> BookIds = BuildBookIds();

  // includeNotes is off by default in the app: the NET license grants quoting of the text
  // "without the NET Bible notes", so the notes are only read once permission is confirmed.
  public static List<ParsedVerse> Parse(string epubPath, bool includeNotes, Action<string>? warn = null)
  {
    using var archive = ZipFile.OpenRead(epubPath);

    var files = archive.Entries
      .Where(e => FileEntryRegex().IsMatch(e.FullName))
      .ToDictionary(e => Path.GetFileName(e.FullName), e => e);

    var verses = new List<ParsedVerse>();
    var seen = new HashSet<(int, int, int)>();

    // Natural order of the file numbers is the order of the Bible.
    foreach (var name in files.Keys
      .Where(n => !n.EndsWith("_notes.xhtml"))
      .OrderBy(n => int.Parse(FileNumberRegex().Match(n).Value)))
    {
      var html = ReadEntry(files[name]);

      if (!TryReadChapterHeader(html, out var bookName, out var chapter, out var bodyStart))
      {
        continue;
      }

      if (!BookIds.TryGetValue(bookName, out var bookId))
      {
        warn?.Invoke($"{name}: unknown book '{bookName}'.");
        continue;
      }

      var notesName = name.Replace(".xhtml", "_notes.xhtml");
      var notes = !includeNotes
        ? null
        : files.TryGetValue(notesName, out var notesEntry)
          ? ParseNotes(ReadEntry(notesEntry))
          : new Dictionary<string, string>();

      var parsed = ParseChapter(bookId, chapter, html[bodyStart..], notes, name, warn);

      // A few chapters open with the end of the previous chapter's last verse, printed
      // with the Hebrew number in brackets, e.g. "(21:1) Then David got up and left...".
      if (parsed.Leading is not null)
      {
        var last = verses.Count > 0 && verses[^1].BookId == bookId ? verses[^1] : null;

        if (last is null)
        {
          warn?.Invoke($"{name}: text before the first verse has no verse to join.");
        }
        else
        {
          verses[^1] = JoinLeading(last, parsed.Leading);
        }
      }

      foreach (var verse in parsed.Verses)
      {
        if (seen.Add((verse.BookId, verse.Chapter, verse.VerseNumber)))
        {
          verses.Add(verse);
        }
        else
        {
          warn?.Invoke($"{name}: duplicate verse {verse.Chapter}:{verse.VerseNumber}.");
        }
      }
    }

    return verses;
  }

  private static string ReadEntry(ZipArchiveEntry entry)
  {
    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
    return reader.ReadToEnd();
  }

  // A chapter file starts "<h1>Genesis<br />Chapter 1</h1>"; Psalms start "<h2>Psalm 2</h2>".
  // Book index pages ("<h2>Genesis</h2>" plus a table of chapters) are not chapters.
  private static bool TryReadChapterHeader(string html, out string book, out int chapter, out int bodyStart)
  {
    book = string.Empty;
    chapter = 0;
    bodyStart = 0;

    var match = ChapterHeaderRegex().Match(html);

    if (match.Success)
    {
      book = WebUtility.HtmlDecode(match.Groups["book"].Value).Trim();
      chapter = int.Parse(match.Groups["chapter"].Value);
      bodyStart = match.Index + match.Length;
      return true;
    }

    match = PsalmHeaderRegex().Match(html);

    if (match.Success)
    {
      book = "Psalms";
      chapter = int.Parse(match.Groups["chapter"].Value);
      bodyStart = match.Index + match.Length;
      return true;
    }

    return false;
  }

  // Notes by the id of their marker in the text (e.g. "n010501"), already converted to the
  // app's footnote format and starting with their type ("TN: ", "SN: " or "TC: ").
  private static Dictionary<string, string> ParseNotes(string html)
  {
    var notes = new Dictionary<string, string>();

    foreach (Match entry in NoteEntryRegex().Matches(html))
    {
      // One marker can carry several notes (e.g. a translation note, then a study note).
      var parts = new List<string>();
      var types = NoteTypeRegex().Matches(entry.Groups["body"].Value);

      for (var i = 0; i < types.Count; i++)
      {
        var start = types[i].Index + types[i].Length;
        var end = i + 1 < types.Count ? types[i + 1].Index : entry.Groups["body"].Length;
        var text = NoteToText(entry.Groups["body"].Value[start..end]);

        if (text.Length > 0)
        {
          parts.Add($"{NoteLabels[types[i].Groups["type"].Value]}: {text}");
        }
      }

      if (parts.Count > 0)
      {
        notes[entry.Groups["id"].Value] = string.Join(" ", parts);
      }
    }

    return notes;
  }

  private static string NoteToText(string html)
  {
    // The app marks italics with * so a literal one (a manuscript's first hand, "C*") must
    // not look like a marker.
    var text = html.Replace('*', '∗');

    text = ItalicRegex().Replace(text, match => $"*{match.Groups[1].Value.Trim()}*");
    text = SuperscriptRegex().Replace(text, match => ToSuperscript(match.Groups[1].Value));

    return Clean(text);
  }

  private static ParsedVerse JoinLeading(ParsedVerse verse, ParsedVerse leading)
  {
    var offset = verse.Footnotes.Count;
    var text = MarkerRegex().Replace(
      HebrewNumberRegex().Replace(leading.Text, string.Empty),
      match => $"[[{int.Parse(match.Groups[1].Value) + offset}]]");

    return verse with
    {
      Text = $"{verse.Text} {text}".Trim(),
      Footnotes = [.. verse.Footnotes, .. leading.Footnotes]
    };
  }

  private sealed record ChapterResult(List<ParsedVerse> Verses, ParsedVerse? Leading);

  private static ChapterResult ParseChapter(
    int bookId,
    int chapter,
    string html,
    Dictionary<string, string>? notes,
    string fileName,
    Action<string>? warn)
  {
    var verses = new List<ParsedVerse>();
    var headingLines = new List<string>();
    var headingNotes = new List<string>();

    var headingLine = new StringBuilder();
    var currentClass = string.Empty;

    VerseBuilder? current = null;
    var leading = new VerseBuilder(0, 0);

    void FinishVerse()
    {
      if (current is not null)
      {
        verses.Add(current.Build(bookId));
      }
    }

    void AddNote(string noteId, bool inHeading)
    {
      if (notes is null)
      {
        return;
      }

      if (!notes.TryGetValue(noteId, out var note))
      {
        warn?.Invoke($"{fileName}: note {noteId} not found.");
        return;
      }

      if (inHeading)
      {
        headingNotes.Add(note);
      }
      else
      {
        (current ?? leading).AddNote(note);
      }
    }

    var position = 0;

    foreach (Match token in TokenRegex().Matches(html))
    {
      AppendText(html[position..token.Index]);
      position = token.Index + token.Length;

      if (token.Groups["open"].Success)
      {
        currentClass = token.Groups["class"].Value;
        headingLine.Clear();
      }
      else if (token.Groups["close"].Success)
      {
        if (HeadingClasses.Contains(currentClass))
        {
          var line = Clean(headingLine.ToString());

          if (line.Length > 0)
          {
            headingLines.Add(line);
          }
        }
        else
        {
          (current ?? leading).EndParagraph();
        }

        currentClass = string.Empty;
        headingLine.Clear();
      }
      else if (token.Groups["verse"].Success)
      {
        FinishVerse();

        var verseChapter = int.Parse(token.Groups["c"].Value);

        if (verseChapter != chapter)
        {
          warn?.Invoke($"{fileName}: verse {verseChapter}:{token.Groups["v"].Value} inside chapter {chapter}.");
        }

        current = new VerseBuilder(verseChapter, int.Parse(token.Groups["v"].Value))
        {
          Heading = headingLines.Count > 0 ? string.Join('\n', headingLines) : null
        };

        headingLines.Clear();

        foreach (var note in headingNotes)
        {
          current.AddNote(note);
        }

        headingNotes.Clear();
      }
      else if (token.Groups["note"].Success)
      {
        AddNote(token.Groups["note"].Value, HeadingClasses.Contains(currentClass));
      }
      else if (token.Groups["smcaps"].Success)
      {
        // Small capitals are kept as the plain words of the EPUB ("Lord", "God"). The EPUB
        // sometimes runs the word into the one before it ("of the<Lord>").
        var name = token.Groups["smcaps"].Value;
        var runsOn = current is not null && current.EndsWithLetter() && name is "Lord" or "God";

        AppendText(runsOn ? " " + name : name, decode: false);
      }
    }

    AppendText(html[position..]);
    FinishVerse();

    var leadingVerse = leading.Build(bookId);

    return new ChapterResult(verses, leadingVerse.Text.Length > 0 ? leadingVerse : null);

    void AppendText(string raw, bool decode = true)
    {
      var text = TagRegex().Replace(raw, string.Empty);

      if (decode)
      {
        text = WebUtility.HtmlDecode(text);
      }

      if (text.Length == 0)
      {
        return;
      }

      if (HeadingClasses.Contains(currentClass))
      {
        headingLine.Append(text);
      }
      else
      {
        (current ?? leading).AppendText(text);
      }
    }
  }

  // The verse being read: its text with [[n]] markers, and its notes.
  private sealed class VerseBuilder(int chapter, int number)
  {
    private readonly StringBuilder _text = new();
    private readonly List<string> _footnotes = [];

    public string? Heading { get; init; }

    public void AppendText(string text) => _text.Append(text);

    public bool EndsWithLetter() => _text.Length > 0 && char.IsLetterOrDigit(_text[^1]);

    public void AddNote(string note)
    {
      _footnotes.Add(note);
      _text.Append($"[[{_footnotes.Count}]]");
    }

    // Poetry is one paragraph per line; lines of a verse run together.
    public void EndParagraph() => _text.Append(' ');

    public ParsedVerse Build(int bookId) => new(
      bookId,
      chapter,
      number,
      Clean(_text.ToString()),
      Heading,
      _footnotes);
  }

  private static string ToSuperscript(string digits)
  {
    const string superscripts = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    return string.Concat(digits.Select(c => c is >= '0' and <= '9' ? superscripts[c - '0'].ToString() : c.ToString()));
  }

  private static string Clean(string html)
  {
    var text = TagRegex().Replace(html, string.Empty);
    text = WebUtility.HtmlDecode(text);
    text = text.Replace(' ', ' ');
    return WhitespaceRegex().Replace(text, " ").Trim();
  }

  private static Dictionary<string, int> BuildBookIds()
  {
    var ids = BibleBookData.Books.ToDictionary(b => b.Name, b => b.Id);
    ids["Song of Solomon"] = ids["Song of Songs"];
    return ids;
  }

  [GeneratedRegex(@"^(?:.*/)?file\d+(?:_notes)?\.xhtml$")]
  private static partial Regex FileEntryRegex();

  [GeneratedRegex(@"\d+")]
  private static partial Regex FileNumberRegex();

  [GeneratedRegex(@"<h1>(?<book>[^<]+)<br\s*/>Chapter (?<chapter>\d+)</h1>")]
  private static partial Regex ChapterHeaderRegex();

  [GeneratedRegex(@"<h2>Psalm (?<chapter>\d+)</h2>")]
  private static partial Regex PsalmHeaderRegex();

  // "<p id="n010501"><a href="...">1</a> <p><b>tn</b> ...</p></p>": the paragraphs are
  // nested, so an entry runs until the next one starts.
  [GeneratedRegex(
    @"<p id=""(?<id>n\d+)""><a [^>]*>\d+</a>(?<body>.*?)(?=<p id=""n\d+"">|</body>)",
    RegexOptions.Singleline)]
  private static partial Regex NoteEntryRegex();

  [GeneratedRegex(@"<b>(?<type>tn|sn|tc)</b>")]
  private static partial Regex NoteTypeRegex();

  [GeneratedRegex(@"<i>(.*?)</i>", RegexOptions.Singleline)]
  private static partial Regex ItalicRegex();

  [GeneratedRegex(@"<sup>(.*?)</sup>", RegexOptions.Singleline)]
  private static partial Regex SuperscriptRegex();

  // Everything that matters in a chapter file, in document order.
  [GeneratedRegex(
    @"<p class=""(?<class>[^""]+)"">(?<open>)"
    + @"|(?<close></p>)"
    + @"|<span class=""verse"">(?<verse>(?<c>\d+):(?<v>\d+))</span>"
    + @"|<sup><a id=""(?<note>n\d+)""[^>]*>\d+</a></sup>"
    + @"|<span class=""smcaps"">(?<smcaps>.*?)</span>",
    RegexOptions.Singleline)]
  private static partial Regex TokenRegex();

  [GeneratedRegex(@"<[^>]+>")]
  private static partial Regex TagRegex();

  [GeneratedRegex(@"\[\[(\d+)\]\]")]
  private static partial Regex MarkerRegex();

  [GeneratedRegex(@"\(\d+:\d+\)\s*")]
  private static partial Regex HebrewNumberRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceRegex();
}
