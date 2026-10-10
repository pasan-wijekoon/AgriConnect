using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly LoginAttemptTracker _loginAttempts;

    public AuthController(AuthService authService, LoginAttemptTracker loginAttempts)
    {
        _authService = authService;
        _loginAttempts = loginAttempts;
    }

    /// <summary>
    /// POST /api/auth/login — Authenticate user
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var client = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (_loginAttempts.RetryAfter(dto.Email, client) is { } wait)
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = "Too many failed sign-in attempts. Please try again later." });
        }

        try
        {
            var result = await _authService.Login(dto);
            _loginAttempts.Reset(dto.Email, client);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _loginAttempts.RecordFailure(dto.Email, client);
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/register — Create new account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.Register(dto);
            return CreatedAtAction(nameof(Me), result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/auth/me — Get current user profile
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var parsed = AuthService.GetUser(User);
        if (parsed == null)
            return Unauthorized(new { error = "Not authenticated." });

        var user = await _authService.GetUserById(parsed.Value.UserId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        return Ok(user);
    }
}
