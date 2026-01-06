using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CatalogOrderApi.DTOs;
using CatalogOrderApi.Services;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // POST: api/auth/google
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleAuth([FromBody] GoogleAuthDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest("ID token is required");
            }

            var (token, user) = await _authService.AuthenticateWithGoogleAsync(request.IdToken);

            var response = new AuthResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    AvatarUrl = user.AvatarUrl
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google authentication failed");
            return Unauthorized("Invalid Google token");
        }
    }

    // GET: api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl
        });
    }
}
