using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile, enrolments and results.</summary>
[ApiController]
[Route("api/users/me")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Gets the logged-in user's profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        return user is null ? NotFound() : UserDto.From(user);
    }

    /// <summary>Updates the logged-in user's full name and phone number.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user is null) return NotFound();
        user.FullName = req.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        await _db.SaveChangesAsync();
        return UserDto.From(user);
    }

    /// <summary>Lists all enrolments for the logged-in participant.</summary>
    [HttpGet("enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMyEnrolments()
    {
        var uid = User.GetUserId();
        var list = await _db.Enrolments.WithDetails()
            .Where(e => e.ParticipantId == uid)
            .OrderByDescending(e => e.EnrolmentDate).ToListAsync();
        return list.Select(EnrolmentDto.From).ToList();
    }

    /// <summary>Lists the logged-in participant's own results (added endpoint).</summary>
    [HttpGet("results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> GetMyResults()
    {
        var uid = User.GetUserId();
        var list = await _db.Results.WithDetails()
            .Where(r => r.Enrolment!.ParticipantId == uid).ToListAsync();
        return list.Select(ResultDto.From).ToList();
    }
}
