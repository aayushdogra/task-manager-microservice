using System.Security.Claims;

namespace TaskManager.Helpers;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("UserId claim not found");

        return Guid.Parse(userId);
    }
}