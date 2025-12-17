using TaskManager.Dto.Auth;
using TaskManager.Helpers;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService auth) =>
        {
            try
            {
                var result = await auth.RegisterAsync(request);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/auth/login", async (LoginRequest request, IAuthService auth) =>
        {
            try
            {
                var result = await auth.LoginAsync(request);
                return Results.Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/auth/refresh", async(RefreshRequest request, IAuthService auth) =>
        {
            var result = await auth.RefreshAsync(request.RefreshToken);
            return Results.Ok(result);
        });

        app.MapGet("/me", async (HttpContext http, IAuthService auth) =>
        {
            var userId = http.User.GetUserId();
            var me = await auth.GetCurrentUserAsync(userId);
            return Results.Ok(me);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");
    }
}