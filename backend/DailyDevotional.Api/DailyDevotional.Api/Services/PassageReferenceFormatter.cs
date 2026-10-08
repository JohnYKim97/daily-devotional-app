namespace DailyDevotional.Api.Services;

// Formats a passage reference the way the ESV does: "John 1:1–18", "Genesis 1:30–2:3",
// "Psalm 23" for a whole chapter, "Jude 3–5" for a one-chapter book. Built from the
// stored range so it works for cached passages and every translation.
public static class PassageReferenceFormatter
{
  private const string EnDash = "–";

  public static string Format(
    string bookName,
    int bookChapterCount,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse,
    int lastVerseOfEndChapter)
  {
    if (bookChapterCount == 1)
    {
      // One-chapter books are cited by verse only, e.g. "Jude 3–5".
      return $"{bookName} {Range(startVerse.ToString(), endVerse.ToString(), startVerse == endVerse)}";
    }

    var wholeChapters = startVerse == 1 && endVerse == lastVerseOfEndChapter;

    if (wholeChapters)
    {
      var name = startChapter == endChapter ? SingularPsalm(bookName) : bookName;
      return $"{name} {Range(startChapter.ToString(), endChapter.ToString(), startChapter == endChapter)}";
    }

    var singleVerse = startChapter == endChapter && startVerse == endVerse;
    var start = $"{startChapter}:{startVerse}";
    var end = startChapter == endChapter ? endVerse.ToString() : $"{endChapter}:{endVerse}";

    return $"{SingularPsalm(bookName)} {Range(start, end, singleVerse)}";
  }

  private static string Range(string start, string end, bool single)
  {
    return single ? start : $"{start}{EnDash}{end}";
  }

  // The book is "Psalms", but one psalm is cited as "Psalm 23".
  private static string SingularPsalm(string bookName)
  {
    return bookName == "Psalms" ? "Psalm" : bookName;
  }
}
