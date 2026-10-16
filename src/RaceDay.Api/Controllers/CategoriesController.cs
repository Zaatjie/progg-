using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Age/distance categories within an event.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists the categories of an event (public).</summary>
    [HttpGet("api/events/{eventId:int}/categories"), AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryDto>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();
        var list = await _db.Categories.Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName).ToListAsync();
        return list.Select(CategoryDto.From).ToList();
    }

    /// <summary>Adds a category to an event. Only the event's Organiser may do this.</summary>
    /// <remarks>Requires categoryName, distance (km), maxParticipants and entryFee.</remarks>
    [HttpPost("api/events/{eventId:int}/categories"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequest req)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var cat = new Category
        {
            EventId = eventId, CategoryName = req.CategoryName.Trim(), Distance = req.Distance,
            MaxParticipants = req.MaxParticipants, EntryFee = req.EntryFee
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", CategoryDto.From(cat));
    }

    /// <summary>Updates a category. Owner of the event only.</summary>
    [HttpPut("api/categories/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest req)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();
        if (cat.Event!.OrganiserId != User.GetUserId()) return Forbid();

        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryId == id);
        if (req.MaxParticipants < enrolled)
            return Conflict(new ProblemDetails
            {
                Title = $"maxParticipants cannot be lower than the {enrolled} participants already enrolled.",
                Status = 409
            });

        cat.CategoryName = req.CategoryName.Trim();
        cat.Distance = req.Distance;
        cat.MaxParticipants = req.MaxParticipants;
        cat.EntryFee = req.EntryFee;
        await _db.SaveChangesAsync();
        return CategoryDto.From(cat);
    }

    /// <summary>Removes a category from an event. Owner of the event only.</summary>
    [HttpDelete("api/categories/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();
        if (cat.Event!.OrganiserId != User.GetUserId()) return Forbid();
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
