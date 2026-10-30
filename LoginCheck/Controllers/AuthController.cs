using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoginCheck.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        try { return Ok(await authService.RegisterAsync(dto)); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        catch (Exception exception) { return StatusCode(500, new { message = "An unexpected error occurred.", details = exception.Message }); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try { return Ok(await authService.LoginAsync(dto)); }
        catch (InvalidOperationException exception) { return Unauthorized(new { message = exception.Message }); }
        catch (Exception exception) { return StatusCode(500, new { message = "An unexpected error occurred.", details = exception.Message }); }
    }
}
