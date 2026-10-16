using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Finish times and positions.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a finish result for an enrolment. Owner of the event only.</summary>
    /// <remarks>Requires finishTime (hh:mm:ss), position and status (Finished, DNF or DNS). One result per enrolment.</remarks>
    [HttpPost("api/enrolments/{id:int}/results"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(int id, ResultRequest req)
    {
        var en = await _db.Enrolments.Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (en is null) return NotFound();
        if (en.Category!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        if (req.Status == ResultStatus.Finished && req.Position < 1)
        {
            ModelState.AddModelError(nameof(req.Position), "Position must be 1 or more for a Finished result.");
            return ValidationProblem(ModelState);
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new ProblemDetails { Title = "A result already exists for this enrolment.", Status = 409 });

        var result = new Result
        {
            EnrolmentId = id, FinishTime = req.FinishTime, Position = req.Position, Status = req.Status!.Value
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await _db.Results.WithDetails().FirstAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/results/{result.ResultId}", ResultDto.From(saved));
    }

    /// <summary>Gets the leaderboard for a category, ordered by position (public).</summary>
    [HttpGet("api/categories/{id:int}/results"), AllowAnonymous]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetLeaderboard(int id)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == id)) return NotFound();
        var list = await _db.Results.WithDetails().Where(r => r.Enrolment!.CategoryId == id).ToListAsync();
        return list.OrderBy(r => r.Status == ResultStatus.Finished ? 0 : 1)
                   .ThenBy(r => r.Position)
                   .Select(ResultDto.From).ToList();
    }

    /// <summary>Updates a previously captured result. Owner of the event only.</summary>
    [HttpPut("api/results/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, ResultRequest req)
    {
        var result = await _db.Results.WithDetails().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound();
        if (result.Enrolment!.Category!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        if (req.Status == ResultStatus.Finished && req.Position < 1)
        {
            ModelState.AddModelError(nameof(req.Position), "Position must be 1 or more for a Finished result.");
            return ValidationProblem(ModelState);
        }
        result.FinishTime = req.FinishTime;
        result.Position = req.Position;
        result.Status = req.Status!.Value;
        await _db.SaveChangesAsync();
        return ResultDto.From(result);
    }
}
