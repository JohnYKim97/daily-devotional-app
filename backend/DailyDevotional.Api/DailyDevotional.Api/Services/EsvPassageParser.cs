using System.Text.RegularExpressions;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Services;

// Parses the plain-text passage format returned by the ESV API when headings and
// footnotes are included:
//
//   John 1:1–18                           <- passage reference (include-passage-references)
//
//   The Word Became Flesh                 <- heading: unindented block with no [n] marker
//
//     [1] In the beginning ... life,(1)   <- verse paragraph; (n) marks a footnote
//
//   Footnotes
//
//   (1) 1:4 Or *was not any thing made*   <- footnote bodies, numbered per passage
public static partial class EsvPassageParser
{
  public static List<ProviderVerse> Parse(string passage, int startChapter)
  {
    var (body, footnoteBodies) = SplitFootnotes(NormalizePassage(passage));

    var verses = new List<ProviderVerse>();
    var currentChapter = startChapter;
    var previousVerseNumber = 0;
    var nextFootnote = 1;
    string? pendingHeading = null;
    var headingLines = new List<string>();

    foreach (var block in BlankLineRegex().Split(body))
    {
      if (string.IsNullOrWhiteSpace(block))
      {
        continue;
      }

      var markers = VerseMarkerRegex().Matches(block);

      if (markers.Count == 0)
      {
        if (!char.IsWhiteSpace(block[0]))
        {
          // Headings are unindented; every unindented block before a verse
          // paragraph belongs to the same heading (title, then superscription).
          headingLines.Add(block.Trim());
          pendingHeading = string.Join('\n', headingLines);
        }
        else if (verses.Count > 0)
        {
          // A continuation of the previous verse, e.g. a later poetry stanza.
          verses[^1] = verses[^1] with { Text = verses[^1].Text + "\n" + block.Trim() };
        }

        continue;
      }

      for (var i = 0; i < markers.Count; i++)
      {
        var start = markers[i].Index + markers[i].Length;
        var end = i + 1 < markers.Count ? markers[i + 1].Index : block.Length;
        var verseNumber = int.Parse(markers[i].Groups[1].Value);
        var text = block[start..end].Trim();

        if (verseNumber < previousVerseNumber)
        {
          currentChapter++;
        }

        previousVerseNumber = verseNumber;

        var footnotes = new List<string>();
        text = FootnoteMarkerRegex().Replace(text, match =>
        {
          // Only the next expected number is a real footnote marker, so ordinary
          // parenthesised digits in the verse text are left alone.
          if (int.Parse(match.Groups[1].Value) != nextFootnote || nextFootnote > footnoteBodies.Count)
          {
            return match.Value;
          }

          footnotes.Add(footnoteBodies[nextFootnote - 1]);
          nextFootnote++;
          return $"[[{footnotes.Count}]]";
        });

        verses.Add(new ProviderVerse(
          currentChapter,
          verseNumber,
          text,
          // The heading belongs to the first verse that follows it.
          i == 0 ? pendingHeading : null,
          footnotes));

        if (i == 0)
        {
          pendingHeading = null;
          headingLines.Clear();
        }
      }
    }

    return verses;
  }

  private static string NormalizePassage(string passage)
  {
    // The short copyright "(ESV)" is returned separately as the translation's notice.
    var text = TrailingCopyrightRegex().Replace(passage.Replace("\r\n", "\n"), string.Empty);

    // With include-passage-references the text starts with the canonical reference
    // (e.g. "John 1:1–18"). That is not a section heading, so drop it.
    return LeadingReferenceRegex().Replace(text, string.Empty, 1);
  }

  private static (string Body, List<string> Footnotes) SplitFootnotes(string passage)
  {
    var split = FootnoteSectionRegex().Match(passage);

    if (!split.Success)
    {
      return (passage, []);
    }

    var footnotes = new List<string>();

    foreach (Match entry in FootnoteEntryRegex().Matches(passage[split.Index..]))
    {
      // "1:4 Or *was not any thing made*": drop the leading "chapter:verse" reference.
      footnotes.Add(FootnoteReferenceRegex().Replace(entry.Groups[2].Value.Trim(), string.Empty, 1));
    }

    return (passage[..split.Index], footnotes);
  }

  [GeneratedRegex(@"\s*\(ESV\)\s*$")]
  private static partial Regex TrailingCopyrightRegex();

  // "John 1:1–18", "1 Corinthians 13", "Genesis 1:30–2:3": a book name followed by chapter/verse digits.
  [GeneratedRegex(@"^\s*[1-3]? ?[A-Za-z][A-Za-z ]*\d+[\d:–—,; \-]*\n[ \t]*\n")]
  private static partial Regex LeadingReferenceRegex();

  [GeneratedRegex(@"\n[ \t]*\n")]
  private static partial Regex BlankLineRegex();

  [GeneratedRegex(@"\[(\d+)\]")]
  private static partial Regex VerseMarkerRegex();

  [GeneratedRegex(@"\((\d+)\)")]
  private static partial Regex FootnoteMarkerRegex();

  [GeneratedRegex(@"\n\s*Footnotes\s*\n(?=\s*\(1\))")]
  private static partial Regex FootnoteSectionRegex();

  [GeneratedRegex(@"^\((\d+)\)[ \t]+(.*?)(?=\n\s*\n\(\d+\)|\s*\z)", RegexOptions.Multiline | RegexOptions.Singleline)]
  private static partial Regex FootnoteEntryRegex();

  [GeneratedRegex(@"^\d+:\d+[a-z]?\s+")]
  private static partial Regex FootnoteReferenceRegex();
}
