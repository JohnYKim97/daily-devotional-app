using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Models;
using Microsoft.AspNetCore.Mvc;
using DailyDevotional.Api.Services.IServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyReadingController : ControllerBase
{
  private readonly IDailyReadingService _readingService;
  private readonly IDailyReadingImportService _importService;
  private readonly IConfiguration _configuration;

  public DailyReadingController(
      IDailyReadingService readingService,
      IDailyReadingImportService importService,
      IConfiguration configuration)
  {
    _readingService = readingService;
    _importService = importService;
    _configuration = configuration;
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

  [Authorize]
  [HttpPost("import")]
  [RequestSizeLimit(10_000_000)]
  public async Task<IActionResult> ImportSchedule(IFormFile file, [FromForm] DateOnly startDate, [FromForm] bool overwrite = false)
  {
    var adminEmail = _configuration["Authentication:AdminEmail"];
    var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);

    if(string.IsNullOrWhiteSpace(adminEmail) || !string.Equals(currentUserEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
    {
      return Forbid();
    }

    if (file == null || file.Length == 0)
    {
      return BadRequest(new { errors = new[] { "No file was uploaded." } });
    }

    if (!Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
    {
      return BadRequest(new { errors = new[] { "Only .docx files are supported." } });
    }

    var tempFilePath = Path.GetTempFileName();

    try
    {
      await using (var stream = System.IO.File.Create(tempFilePath))
      {
        await file.CopyToAsync(stream);
      }

      List<ParsedReading> parsedReadings;

      try
      {
        parsedReadings = _importService.ParseDocument(tempFilePath);
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { errors = new[] { ex.Message } });
      }

      if (parsedReadings.Count == 0)
      {
        return BadRequest(new { errors = new[] { "No readings were found in the document." } });
      }

      var validationErrors = _importService.ValidateReadings(parsedReadings);

      if (validationErrors.Count > 0)
      {
        return BadRequest(new { errors = validationErrors });
      }

      try
      {
        await _importService.ResolveVerseRangesAsync(parsedReadings);
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { errors = new[] { ex.Message } });
      }

      var readings = _importService.CreateDailyReadings(parsedReadings, startDate);

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

