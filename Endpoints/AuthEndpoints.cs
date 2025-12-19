using FluentValidation;
using System.ComponentModel.DataAnnotations;
using TaskManager.Dto.Auth;
using TaskManager.Helpers;
using TaskManager.RateLimiting;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest? request, IValidator < RegisterRequest > validator, IAuthService auth) =>
        {
            if(request is null)
                return Results.BadRequest(new { error = "Request body is required" });

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            try
            {
                var result = await auth.RegisterAsync(request);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithMetadata(new RequireRateLimitingAttribute());

        app.MapPost("/auth/login", async (LoginRequest request, IValidator<LoginRequest> validator, IAuthService auth) =>
        {
            if (request is null) 
                return Results.BadRequest(new { error = "Request body is required" });

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            try
            {
                var result = await auth.LoginAsync(request);
                return Results.Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).WithMetadata(new RequireRateLimitingAttribute());

        app.MapPost("/auth/refresh", async (RefreshRequest request, IValidator<RefreshRequest> validator, IAuthService auth) =>
        {
            if (request is null)
                return Results.BadRequest(new { error = "Request body is required" });

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            try
            {
                var result = await auth.RefreshAsync(request.RefreshToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).WithMetadata(new RequireRateLimitingAttribute());

        app.MapPost("/auth/logout", async (RefreshRequest request, IValidator<RefreshRequest> validator, IAuthService auth) =>
        {
            if (request is null)
                return Results.BadRequest(new { error = "Request body is required" });

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            await auth.LogoutAsync(request.RefreshToken);
            return Results.NoContent();

        }).WithMetadata(new RequireRateLimitingAttribute());

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