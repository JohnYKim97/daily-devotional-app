using DailyDevotional.Api.Services;
using DailyDevotional.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyReadingController : ControllerBase
{
  private readonly IDailyReadingService _readingService;

  public DailyReadingController(IDailyReadingService readingService)
  {
    _readingService = readingService;
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
}
