using DailyDevotional.Api.Models;
using DailyDevotional.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DailyDevotional.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly JwtService _jwtService;

  public AuthController(UserManager<ApplicationUser> userManager, JwtService jwtService)
  {
    _userManager = userManager;
    _jwtService = jwtService;
  }

  [HttpGet("google")]
  public IActionResult GoogleLogin()
  {
    var properties =
      new AuthenticationProperties
      {
        RedirectUri = "/api/auth/google-success"
      };

    return Challenge(
      properties,
      GoogleDefaults.AuthenticationScheme);
  }

  [HttpGet("google-success")]
  public async Task<IActionResult> GoogleSuccess()
  {
    var authenticateResult =
        await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

    if (!authenticateResult.Succeeded ||
        authenticateResult.Principal == null)
    {
      return Unauthorized();
    }

    var principal = authenticateResult.Principal;

    // The Google cookie has served its purpose.
    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    var email = principal.FindFirstValue(
        ClaimTypes.Email);

    if (string.IsNullOrEmpty(email))
    {
      return BadRequest(
          "Google account did not provide an email address.");
    }

    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
    {
      user = new ApplicationUser
      {
        UserName = email,
        Email = email,
        EmailConfirmed = true
      };

      var result = await _userManager.CreateAsync(user);

      if (!result.Succeeded)
      {
        return BadRequest(new
        {
          errors = result.Errors.Select(
                error => error.Description)
        });
      }
    }

    var token = _jwtService.GenerateToken(user);

    return Ok(new
    {
      token,
      userId = user.Id,
      email = user.Email,
      name = principal.Identity?.Name
    });
  }

  [Authorize]
  [HttpGet("me")]
  public async Task<IActionResult> GetCurrentUser()
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
    {
      return Unauthorized();
    }

    var user = await _userManager.FindByIdAsync(userId);

    if (user == null)
    {
      return Unauthorized();
    }

    return Ok(new
    {
      userId = user.Id,
      email = user.Email
    });
  }
}
