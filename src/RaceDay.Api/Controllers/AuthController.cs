using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using AppUser = RaceDay.Api.Models.User;

namespace RaceDay.Api.Controllers;

/// <summary>Registration and login (public endpoints).</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, ITokenService tokens, IConfiguration config)
    {
        _db = db; _tokens = tokens; _config = config;
    }

    /// <summary>Registers a new Organiser or Participant.</summary>
    /// <remarks>
    /// Expects fullName, email, password (min 8 chars) and role (Organiser or Participant).
    /// Registering as an Organiser also requires the secret organiserCode, so that
    /// not just anyone can give themselves organiser powers.
    /// Returns the new user (never the password).
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest req)
    {
        if (req.Role == UserRole.Organiser)
        {
            var expected = _config["Registration:OrganiserCode"];
            if (string.IsNullOrEmpty(expected) || req.OrganiserCode != expected)
            {
                ModelState.AddModelError(nameof(req.OrganiserCode),
                    "A valid organiser code is required to register as an Organiser.");
                return ValidationProblem(ModelState);
            }
        }

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ProblemDetails { Title = "Email already registered.", Status = 409 });

        var user = new AppUser
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role!.Value
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, UserDto.From(user));
    }

    /// <summary>Logs in and returns a JWT access token.</summary>
    /// <remarks>Send the token as 'Authorization: Bearer {token}' (use the Authorize button in Swagger).</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password.", Status = 401 });

        return Ok(new AuthResponse { Token = _tokens.CreateToken(user), User = UserDto.From(user) });
    }
}
