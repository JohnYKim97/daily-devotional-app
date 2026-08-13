using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalController : ControllerBase
{
  private readonly IJournalService _journalService;

  public JournalController(IJournalService journalService)
  {
    _journalService = journalService;
  }

  [HttpPost]
  public async Task<ActionResult<JournalResponse>> CreateJournal(CreateJournalRequest request)
  {
    var journal = await _journalService.CreateJournalAsync(request);

    return Ok(journal);
  }

  [HttpGet("{date}")]
  public async Task<ActionResult<JournalResponse>> GetJournal(
    DateOnly date)
  {
    var journal = await _journalService.GetJournalByDateAsync(date);

    if (journal == null)
    {
      return NotFound();
    }

    return Ok(journal);
  }

  [HttpPut("{date}")]
  public async Task<ActionResult<JournalResponse>> UpdateJournal(DateOnly
     date, UpdateJournalRequest request)
  {
    var journal = await _journalService.UpdateJournalAsync(date, request);

    if (journal == null)
    {
      return NotFound();
    }

    return Ok(journal);
  }
}
