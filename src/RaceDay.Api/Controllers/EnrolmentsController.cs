using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering categories, and organisers viewing who entered.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in participant into a category.</summary>
    /// <remarks>
    /// No body needed. The enrolment links the participant, the category and (through the category) the event.
    /// Returns 409 if the participant is already enrolled or the category is full.
    /// </remarks>
    [HttpPost("api/categories/{id:int}/enrolments"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return NotFound();

        var uid = User.GetUserId();
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id && e.ParticipantId == uid))
            return Conflict(new ProblemDetails { Title = "You are already enrolled in this category.", Status = 409 });

        var count = await _db.Enrolments.CountAsync(e => e.CategoryId == id);
        if (count >= cat.MaxParticipants)
            return Conflict(new ProblemDetails { Title = "This category is full.", Status = 409 });

        var enrolment = new Enrolment { ParticipantId = uid, CategoryId = id };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await _db.Enrolments.WithDetails().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created($"/api/users/me/enrolments", EnrolmentDto.From(saved));
    }

    /// <summary>Lists all participants enrolled in a category. Owner of the event only.</summary>
    [HttpGet("api/categories/{id:int}/enrolments"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetForCategory(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();
        if (cat.Event!.OrganiserId != User.GetUserId()) return Forbid();
        var list = await _db.Enrolments.WithDetails().Where(e => e.CategoryId == id).ToListAsync();
        return list.Select(EnrolmentDto.From).ToList();
    }

    /// <summary>Cancels (deletes) an enrolment. Only the participant who made it may do this.</summary>
    [HttpDelete("api/enrolments/{id:int}"), Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var en = await _db.Enrolments.FindAsync(id);
        if (en is null) return NotFound();
        if (en.ParticipantId != User.GetUserId()) return Forbid();
        _db.Enrolments.Remove(en);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
