using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Events. Anyone can view; only the owning Organiser can change them.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists events, optionally filtered by date and/or location (public).</summary>
    /// <param name="date">Only events on this date (yyyy-MM-dd).</param>
    /// <param name="location">Only events whose location contains this text.</param>
    [HttpGet("api/events"), AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDto>>> GetAll([FromQuery] DateTime? date, [FromQuery] string? location)
    {
        var q = _db.Events.Include(e => e.Organiser).AsQueryable();
        if (date.HasValue) { var d = date.Value.Date; q = q.Where(e => e.EventDate == d); }
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(e => e.Location.Contains(location));
        var list = await q.OrderBy(e => e.EventDate).ToListAsync();
        return list.Select(e => EventDto.From(e)).ToList();
    }

    /// <summary>Gets one event including its categories (public).</summary>
    [HttpGet("api/events/{id:int}"), AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? NotFound() : EventDto.From(ev, includeCategories: true);
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    /// <remarks>Requires eventName, eventDate, location, eventType (Run, Walk or Cycle) and distance (km).</remarks>
    [HttpPost("api/events"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create(EventRequest req)
    {
        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            EventName = req.EventName.Trim(),
            EventDate = req.EventDate!.Value.Date,
            Location = req.Location.Trim(),
            Description = req.Description?.Trim(),
            EventType = req.EventType!.Value,
            Distance = req.Distance
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, EventDto.From(ev));
    }

    /// <summary>Updates an event. Only the Organiser who owns it may do this.</summary>
    [HttpPut("api/events/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, EventRequest req)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        ev.EventName = req.EventName.Trim();
        ev.EventDate = req.EventDate!.Value.Date;
        ev.Location = req.Location.Trim();
        ev.Description = req.Description?.Trim();
        ev.EventType = req.EventType!.Value;
        ev.Distance = req.Distance;
        await _db.SaveChangesAsync();
        return EventDto.From(ev);
    }

    /// <summary>Deletes an event (and its categories, enrolments and results). Owner only.</summary>
    [HttpDelete("api/events/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Lists every enrolment across all categories of one event. Owner only (added endpoint).</summary>
    [HttpGet("api/events/{id:int}/enrolments"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetEventEnrolments(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();
        var list = await _db.Enrolments.WithDetails()
            .Where(e => e.Category!.EventId == id).ToListAsync();
        return list.Select(EnrolmentDto.From).ToList();
    }
}
