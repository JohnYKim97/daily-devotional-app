using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

// Turns the HTML the YouVersion Platform API returns for a passage
// (GET /v1/bibles/{id}/passages/{ref}?format=html&include_headings=true&include_notes=true)
// into verses.
//
// The HTML is a flat list of <div class="..."> blocks:
//   section headings    <div class="s1 yv-h">, "s", "ms", "r", ...   (every "yv-h" block)
//   chapter label       <div class="cl yv-h">Psalm 23</div>
//   Psalm title         <div class="d">A psalm of David.</div>
//   verse text          <div class="p|q1|q2|...">, each verse starting with
//                       <span class="yv-v" v="16"></span><span class="yv-vlbl">16</span>
//   footnote            <span class="yv-n f"><span class="fr">23:4 </span><span class="ft">Or </span>...</span>
//
// Output follows the format the verse list shows for the ESV: [[n]] markers in the text, n
// being the position in the footnote list, *asterisks* for italics in a note, and headings as
// lines (title, then subtitles). Poetry lines are joined into one paragraph. The words
// themselves are kept exactly as supplied (including the NASB's literal asterisks, which are
// part of its text); only the typography (italics, red letters, small capitals) is not shown.
public static partial class YouVersionPassageParser
{
  private const char MarkerStart = '\u0001';
  private const char MarkerEnd = '\u0002';

  // Blocks that are headings although they are not marked "yv-h".
  private static readonly HashSet<string> HeadingClasses = ["d", "sp", "r", "mr", "qa", "cl"];

  public static List<ProviderVerse> Parse(string html, int chapter)
  {
    var footnotes = new List<string>();

    html = ExtractFootnotes(html, footnotes);

    var verses = new List<VerseBuilder>();
    var headingLines = new List<string>();
    var headingMarkers = new StringBuilder();

    VerseBuilder? current = null;

    foreach (Match block in BlockRegex().Matches(html))
    {
      var classes = block.Groups["class"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var body = block.Groups["body"].Value;

      var isHeading = classes.Contains("yv-h") || classes.Any(HeadingClasses.Contains);

      if (isHeading)
      {
        var text = Clean(MarkerRegex().Replace(body, string.Empty));

        headingMarkers.Append(string.Concat(MarkerRegex().Matches(body).Select(m => m.Value)));

        if (text.Length > 0)
        {
          headingLines.Add(text);
        }

        continue;
      }

      // A verse can start anywhere inside a block (poetry lines carry several).
      var offset = 0;

      foreach (Match verseStart in VerseStartRegex().Matches(body))
      {
        current?.Append(body[offset..verseStart.Index]);

        current = new VerseBuilder(chapter, int.Parse(verseStart.Groups["v"].Value))
        {
          Heading = headingLines.Count > 0 ? string.Join('\n', headingLines) : null
        };
        current.Append(string.Concat(headingMarkers.ToString()));

        headingLines.Clear();
        headingMarkers.Clear();
        verses.Add(current);

        offset = verseStart.Index + verseStart.Length;
      }

      current?.Append(body[offset..]);
      current?.EndBlock();
    }

    return verses
      .Select(v => v.Build(footnotes))
      .Where(v => v is not null)
      .Select(v => v!)
      .ToList();
  }

  // Replaces each note by a marker holding its index and collects its text.
  private static string ExtractFootnotes(string html, List<string> footnotes)
  {
    var result = new StringBuilder();
    var position = 0;

    foreach (Match start in NoteStartRegex().Matches(html))
    {
      if (start.Index < position)
      {
        continue;
      }

      result.Append(html, position, start.Index - position);

      var bodyStart = start.Index + start.Length;
      var end = FindClosingSpan(html, bodyStart);

      footnotes.Add(NoteToText(html[bodyStart..end]));
      result.Append(MarkerStart).Append(footnotes.Count - 1).Append(MarkerEnd);

      position = Math.Min(end + "</span>".Length, html.Length);
    }

    result.Append(html, position, html.Length - position);

    return result.ToString();
  }

  // The index of the </span> that closes a span whose content starts at 'from'.
  private static int FindClosingSpan(string html, int from)
  {
    var depth = 1;

    foreach (Match tag in SpanTagRegex().Matches(html, from))
    {
      depth += tag.Value == "</span>" ? -1 : 1;

      if (depth == 0)
      {
        return tag.Index;
      }
    }

    return html.Length;
  }

  private static string NoteToText(string html)
  {
    // The leading "23:4" names the verse the note belongs to.
    html = NoteReferenceRegex().Replace(html, string.Empty);

    // A literal * would look like an italics marker.
    html = html.Replace('*', '∗');

    // Alternative readings, quoted words and added words are shown in italics.
    html = ItalicSpanRegex().Replace(html, match =>
    {
      var text = match.Groups["text"].Value;
      var core = Clean(text);

      // Keep a space the span carried on either side ("<fq>atonement </fq>refers").
      return core.Length == 0
        ? text
        : $"{(text.Length > 0 && char.IsWhiteSpace(text[0]) ? " " : "")}*{core}*{(char.IsWhiteSpace(text[^1]) ? " " : "")}";
    });

    return Clean(html);
  }

  private static string Clean(string html)
  {
    var text = TagRegex().Replace(html, string.Empty);
    text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
    return WhitespaceRegex().Replace(text, " ").Trim();
  }

  private sealed class VerseBuilder(int chapter, int number)
  {
    private readonly StringBuilder _text = new();

    public string? Heading { get; init; }

    public void Append(string html) => _text.Append(TagRegex().Replace(html, string.Empty));

    // Poetry is one block per line; lines of a verse run together.
    public void EndBlock() => _text.Append(' ');

    public ProviderVerse? Build(List<string> footnotes)
    {
      var text = WebUtility.HtmlDecode(_text.ToString()).Replace(' ', ' ');
      text = WhitespaceRegex().Replace(text, " ").Trim();

      if (!MarkerRegex().Replace(text, string.Empty).Any(char.IsLetterOrDigit))
      {
        return null;
      }

      // Number the markers in the order they appear in this verse.
      var ordered = new List<string>();

      text = MarkerRegex().Replace(text, match =>
      {
        ordered.Add(footnotes[int.Parse(match.Groups[1].Value)]);
        return $"[[{ordered.Count}]]";
      });

      return new ProviderVerse(chapter, number, text, Heading, ordered);
    }
  }

  [GeneratedRegex(@"<div class=""(?<class>[^""]*)"">(?<body>.*?)</div>", RegexOptions.Singleline)]
  private static partial Regex BlockRegex();

  [GeneratedRegex(@"<span class=""yv-v"" v=""(?<v>\d+)""></span>(?:<span class=""yv-vlbl"">[^<]*</span>)?")]
  private static partial Regex VerseStartRegex();

  // Footnotes ("f") and cross references ("x") are both notes to keep.
  [GeneratedRegex(@"<span class=""yv-n [a-z]+"">")]
  private static partial Regex NoteStartRegex();

  [GeneratedRegex(@"<span class=""fr"">.*?</span>", RegexOptions.Singleline)]
  private static partial Regex NoteReferenceRegex();

  [GeneratedRegex(@"<span class=""(?:fq|fqa|it)"">(?<text>.*?)</span>", RegexOptions.Singleline)]
  private static partial Regex ItalicSpanRegex();

  [GeneratedRegex(@"<span\b[^>]*>|</span>")]
  private static partial Regex SpanTagRegex();

  [GeneratedRegex("\u0001(\\d+)\u0002")]
  private static partial Regex MarkerRegex();

  [GeneratedRegex(@"<[^>]+>")]
  private static partial Regex TagRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceRegex();
}
