using DailyDevotional.Api.Models;

namespace DailyDevotional.Api.Services.IServices;

public record ProviderVerse(
  int Chapter,
  int VerseNumber,
  string Text,
  string? Heading = null,
  List<string>? Footnotes = null);

// Fetches verse text from an external API for translations whose license
// only allows a limited, expiring cache (StorageMode = Cache).
public interface ITranslationProvider
{
  TranslationProviderKind Kind { get; }

  Task<List<ProviderVerse>> GetVersesAsync(
    Translation translation,
    string bookName,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse,
    // Verse count of every chapter in the range, for providers that must split a request.
    IReadOnlyDictionary<int, int> chapterVerseCounts);

  // The translation's current copyright notice from the provider, when it publishes one
  // (see Translation.NoticeFromProvider). Null when it does not.
  Task<string?> GetCopyrightNoticeAsync(Translation translation) => Task.FromResult<string?>(null);
}
