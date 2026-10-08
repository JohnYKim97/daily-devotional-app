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
    string bookName,
    int startChapter,
    int startVerse,
    int endChapter,
    int endVerse);
}
