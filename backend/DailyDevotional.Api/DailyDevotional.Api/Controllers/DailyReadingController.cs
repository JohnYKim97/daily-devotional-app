using DailyDevotional.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services.IServices;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyReadingController : ControllerBase
{
  private readonly IDailyReadingService _readingService;
  private readonly IDailyReadingImportService _importService;

  public DailyReadingController(IDailyReadingService readingService, IDailyReadingImportService importService)
  {
    _readingService = readingService;
    _importService = importService;
  }

  [HttpGet("{date}")]
  public async Task<ActionResult<DailyReadingResponse>> GetReading(DateOnly date)
  {
    var reading = await _readingService.GetReadingByDateAsync(date);

    if (reading == null) {
      return NotFound();
    }

    return Ok(reading);
  }

  [HttpPost("{id}/import-verses")]
  public async Task<IActionResult> ImportVerses(int id)
  {
    var success = await _readingService.ImportVersesAsync(id);

    if (!success)
    {
      return NotFound();
    }

    return Ok();
  }

  [HttpPost("import")]
  [RequestSizeLimit(10_000_000)]
  public async Task<IActionResult> ImportSchedule(IFormFile file, [FromForm] DateOnly startDate, [FromForm] bool overwrite = false)
  {
    if (file == null || file.Length == 0)
    {
      return BadRequest(new { error = new[] { "No file was uploaded." } });
    }

    if (!Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
    {
      return BadRequest(new { errors = new[] { "Only .docx files are support." } });
    }

    var tempFilePath = Path.GetTempFileName();

    try
    {
      await using (var stream = System.IO.File.Create(tempFilePath))
      {
        await file.CopyToAsync(stream);
      }

      List<ParsedReading> parseReadings;

      try
      {
        parseReadings = _importService.ParseDocument(tempFilePath);
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { errors = new[] { ex.Message } });
      }

      if (parseReadings.Count == 0)
      {
        return BadRequest(new { errors = new[] { "No readings were found in the document." } });
      }

      var validationErrors = _importService.ValidateReadings(parseReadings);

      if (validationErrors.Count > 0)
      {
        return BadRequest(new { errors = validationErrors });
      }

      try
      {
        await _importService.ResolveVerseRangesAsync(parseReadings);
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { errors = new[] {ex.Message } });
      }

      var readings = _importService.CreateDailyReadings(parseReadings, startDate);

      var result = await _readingService.SaveImportedReadingsAsync(readings, overwrite);

      return Ok(result);
    }
    finally
    {
      if (System.IO.File.Exists(tempFilePath))
      {
        System.IO.File.Delete(tempFilePath);
      }
    }

  }
}

