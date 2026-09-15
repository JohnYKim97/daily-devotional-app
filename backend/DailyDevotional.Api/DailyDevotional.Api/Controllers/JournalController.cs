using DailyDevotional.Api.DTOs;
using DailyDevotional.Api.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalController : ControllerBase
{
  private readonly IJournalService _journalService;

  public JournalController(IJournalService journalService)
  {
    _journalService = journalService;
  }

  private string GetCurrentUserId()
  {
    return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  }

  [HttpPost]
  public async Task<ActionResult<JournalResponse>> CreateJournal(CreateJournalRequest request)
  {
    var journal = await _journalService.CreateJournalAsync(GetCurrentUserId(),request);

    return Ok(journal);
  }

  [HttpGet("{date}")]
  public async Task<ActionResult<JournalResponse>> GetJournal(DateOnly date)
  {
    var journal = await _journalService.GetJournalByDateAsync(GetCurrentUserId(), date);

    if (journal == null)
    {
      return NotFound();
    }

    return Ok(journal);
  }

  [HttpPut("{date}")]
  public async Task<ActionResult<JournalResponse>> UpdateJournal(DateOnly date, UpdateJournalRequest request)
  {
    var journal = await _journalService.UpdateJournalAsync(GetCurrentUserId(), date, request);

    if (journal == null)
    {
      return NotFound();
    }

    return Ok(journal);
  }

  [HttpGet]
  public async Task<ActionResult<List<JournalHistoryEntryResponse>>> GetAllJournals()
  {
    var journals = await _journalService.GetAllJournalsAsync(GetCurrentUserId());

    return Ok(journals);
  }
}
