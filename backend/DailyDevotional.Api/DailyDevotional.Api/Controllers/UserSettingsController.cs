using DailyDevotional.Api.Services.IServices;
using DailyDevotional.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
  private readonly IUserSettingsService _userSettingsService;

  public UserSettingsController(IUserSettingsService userSettingsService)
  {
    _userSettingsService = userSettingsService;
  }

  private string GetCurrentUserId()
  {
    return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  }

  [HttpGet]
  public async Task<ActionResult<UserSettingsResponse>> GetSettings()
  {
    return await _userSettingsService.GetSettingsAsync(GetCurrentUserId());
  }

  [HttpPut]
  public async Task<ActionResult<UserSettingsResponse>> UpdateSettings(UpdateUserSettingsRequest request)
  {
    var settings = await _userSettingsService.UpdateSettingsAsync(GetCurrentUserId(), request);

    return Ok(settings);
  }

}
