namespace DailyDevotional.Api.Services;

// Thrown when a passage is longer than a translation's license allows to be shown at once.
// It derives from ArgumentException so the passage endpoint answers it with a 400 and the
// message, while the daily reading catches it to show the reading without verse text.
public class PassageTooLongException : ArgumentException
{
  public PassageTooLongException(string message) : base(message)
  {
  }
}
