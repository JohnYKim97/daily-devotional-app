namespace DailyDevotional.Api.Services.IServices;

public interface IAiCommentaryService
{
  Task<string> GenerateCommentaryAsync(
    string book,
    int chapter,
    int endChapter,
    int startVerse,
    int endVerse,
    string passageText);
}
