using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public static class Queries
{
    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> q) =>
        q.Include(e => e.Participant)
         .Include(e => e.Category).ThenInclude(c => c!.Event);

    public static IQueryable<Result> WithDetails(this IQueryable<Result> q) =>
        q.Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
         .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event);
}
