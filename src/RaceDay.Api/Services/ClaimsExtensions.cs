using System.Security.Claims;

namespace RaceDay.Api.Services;

public static class ClaimsExtensions
{
    /// <summary>Returns the logged-in user's id from the JWT.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? throw new InvalidOperationException("No user id claim."));
}
