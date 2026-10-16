using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

// Turns the HTML the NLT API (https://api.nlt.to/api/passages) returns into verses.
//
// Each verse is a <verse_export ch="" vn=""> element. Inside it:
//   <span class="vn">     the verse number (dropped; the number comes from the attributes)
//   <h3|h4 class="subhead">, <h5 class="psa-hebrew">, <p class="psa-title">   headings
//   <h2|h3 class="chapter-number">                                           chapter title (dropped)
//   <a class="a-tn">*</a><span class="tn"><span class="tn-ref">3:15</span> note</span>   a footnote
//   <h3 class="text-critical">                                               editorial bracket, kept in the text
//
// Output follows the format the verse list shows for the ESV: [[n]] markers in the text, n
// being the position in the footnote list, *asterisks* for italics in a note, and headings as
// lines (title, then subtitles) in the heading. Poetry lines are joined into one paragraph.
// Small capitals (the divine name) stay plain words ("Lord"), as in the NLT's own text.
public static partial class NltPassageParser
{
  private const char MarkerStart = '\u0001';
  private const char MarkerEnd = '\u0002';

  public static List<ProviderVerse> Parse(string html)
  {
    var verses = new List<ProviderVerse>();

    foreach (Match export in VerseExportRegex().Matches(html))
    {
      var verse = ParseVerse(
        int.Parse(export.Groups["ch"].Value),
        int.Parse(export.Groups["vn"].Value),
        export.Groups["body"].Value);

      // Verses the NLT leaves out (e.g. Acts 8:37) come back as empty elements.
      if (verse is not null)
      {
        verses.Add(verse);
      }
    }

    return verses;
  }

  private static ProviderVerse? ParseVerse(int chapter, int number, string html)
  {
    var footnotes = new List<string>();

    html = ExtractFootnotes(html, footnotes);

    var leadingMarkers = new StringBuilder();
    var headingLines = new List<string>();

    html = ChapterTitleRegex().Replace(html, match =>
    {
      // A note on the chapter title (e.g. Psalm 119) is shown from the start of the verse.
      leadingMarkers.Append(string.Concat(MarkerRegex().Matches(match.Value).Select(m => m.Value)));
      return string.Empty;
    });

    html = HeadingRegex().Replace(html, match =>
    {
      var text = Clean(MarkerRegex().Replace(match.Groups["text"].Value, string.Empty));

      leadingMarkers.Append(string.Concat(MarkerRegex().Matches(match.Groups["text"].Value).Select(m => m.Value)));

      if (text.Length > 0)
      {
        headingLines.Add(text);
      }

      return string.Empty;
    });

    html = VerseNumberRegex().Replace(html, string.Empty);
    html = BlockBreakRegex().Replace(html, " ");

    var text = Clean($"{leadingMarkers} {html}");

    if (!MarkerRegex().Replace(text, string.Empty).Any(char.IsLetterOrDigit))
    {
      return null;
    }

    // Number the markers in the order they appear.
    var ordered = new List<string>();

    text = MarkerRegex().Replace(text, match =>
    {
      ordered.Add(footnotes[int.Parse(match.Groups[1].Value)]);
      return $"[[{ordered.Count}]]";
    });

    return new ProviderVerse(
      chapter,
      number,
      text,
      headingLines.Count > 0 ? string.Join('\n', headingLines) : null,
      ordered);
  }

  // Replaces each footnote by a marker holding its index and collects its text. The note is
  // a span that can hold other spans, so it ends at the matching closing tag.
  private static string ExtractFootnotes(string html, List<string> footnotes)
  {
    const string opening = "<a class=\"a-tn\">*</a><span class=\"tn\">";

    var result = new StringBuilder();
    var position = 0;

    while (true)
    {
      var start = html.IndexOf(opening, position, StringComparison.Ordinal);

      if (start < 0)
      {
        result.Append(html, position, html.Length - position);
        break;
      }

      result.Append(html, position, start - position);

      var bodyStart = start + opening.Length;
      var end = FindClosingSpan(html, bodyStart);

      var body = html[bodyStart..end];

      footnotes.Add(NoteToText(body));
      result.Append(MarkerStart).Append(footnotes.Count - 1).Append(MarkerEnd);

      position = Math.Min(end + "</span>".Length, html.Length);
    }

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
    // The leading "3:15" names the verse the note belongs to.
    html = NoteReferenceRegex().Replace(html, string.Empty);

    // A literal * would look like an italics marker.
    html = html.Replace('*', '∗');
    html = ItalicRegex().Replace(html, match => $"*{match.Groups[1].Value.Trim()}*");

    return Clean(html);
  }

  private static string Clean(string html)
  {
    var text = TagRegex().Replace(html, string.Empty);
    text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
    return WhitespaceRegex().Replace(text, " ").Trim();
  }

  [GeneratedRegex(
    @"<verse_export [^>]*?ch=""(?<ch>\d+)"" vn=""(?<vn>\d+)"">(?<body>.*?)</verse_export>",
    RegexOptions.Singleline)]
  private static partial Regex VerseExportRegex();

  [GeneratedRegex(@"<h[1-6] class=""chapter-number"">.*?</h[1-6]>", RegexOptions.Singleline)]
  private static partial Regex ChapterTitleRegex();

  // Section headings, Hebrew letter headings and Psalm titles, in the order they appear.
  [GeneratedRegex(
    @"<h(?<level>[3-5]) class=""(?:subhead|psa-hebrew)"">(?<text>.*?)</h\k<level>>"
    + @"|<p class=""psa-title"">(?<text>.*?)</p>",
    RegexOptions.Singleline)]
  private static partial Regex HeadingRegex();

  [GeneratedRegex(@"<span class=""vn"">\d+</span>")]
  private static partial Regex VerseNumberRegex();

  // Paragraph and line boundaries become spaces, so poetry lines run together.
  [GeneratedRegex(@"</?p(?: [^>]*)?>|<br\s*/?>|<hr[^>]*>|</?h[1-6][^>]*>")]
  private static partial Regex BlockBreakRegex();

  [GeneratedRegex(@"<span class=""tn-ref"">.*?</span>\s*", RegexOptions.Singleline)]
  private static partial Regex NoteReferenceRegex();

  [GeneratedRegex(@"<(?:em|i)>(.*?)</(?:em|i)>", RegexOptions.Singleline)]
  private static partial Regex ItalicRegex();

  [GeneratedRegex(@"<span\b[^>]*>|</span>")]
  private static partial Regex SpanTagRegex();

  [GeneratedRegex("\u0001(\\d+)\u0002")]
  private static partial Regex MarkerRegex();

  [GeneratedRegex(@"<[^>]+>")]
  private static partial Regex TagRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceRegex();
}
