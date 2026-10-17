using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.Services.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationsController : ControllerBase
{
  private readonly IBibleMetadataService _metadataService;
  private readonly IBibleTextService _bibleTextService;

  public TranslationsController(IBibleMetadataService metadataService, IBibleTextService bibleTextService)
  {
    _metadataService = metadataService;
    _bibleTextService = bibleTextService;
  }

  [HttpGet]
  public async Task<ActionResult<List<TranslationResponse>>> GetTranslations()
  {
    return Ok(await _metadataService.GetTranslationsAsync());
  }

  [HttpGet("{code}/books")]
  public async Task<ActionResult<List<BookResponse>>> GetBooks(string code)
  {
    var books = await _metadataService.GetBooksAsync(code);

    return books == null ? NotFound() : Ok(books);
  }

  [HttpGet("{code}/books/{bookId}/chapters")]
  public async Task<ActionResult<List<ChapterResponse>>> GetChapters(string code, int bookId)
  {
    var chapters = await _metadataService.GetChaptersAsync(code, bookId);

    return chapters == null ? NotFound() : Ok(chapters);
  }

  [EnableRateLimiting("passages")]
  [HttpGet("{code}/passage")]
  public async Task<ActionResult<PassageResponse>> GetPassage(
    string code,
    [FromQuery] int bookId,
    [FromQuery] int startChapter,
    [FromQuery] int? startVerse,
    [FromQuery] int? endChapter,
    [FromQuery] int? endVerse)
  {
    var lastChapter = endChapter ?? startChapter;

    try
    {
      var passage = await _bibleTextService.GetPassageAsync(
        code,
        bookId,
        startChapter,
        startVerse,
        lastChapter,
        endVerse);

      return Ok(passage);
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { errors = new[] { ex.Message } });
    }
    catch (ProviderRateLimitException ex)
    {
      Response.Headers.RetryAfter = Math.Ceiling(ex.RetryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
      return StatusCode(StatusCodes.Status429TooManyRequests, new { errors = new[] { "The Bible text service is busy right now. Please try again shortly." } });
    }
    catch (HttpRequestException)
    {
      return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "The passage could not be retrieved right now. Please try again shortly." } });
    }
  }
}
