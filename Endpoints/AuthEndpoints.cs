using FluentValidation;
using TaskManager.Dto.Auth;
using TaskManager.Helpers;
using TaskManager.RateLimiting;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // REGISTER
        app.MapPost("/auth/register", async (RegisterRequest? request, IValidator < RegisterRequest > validator, IAuthService auth) =>
        {
            if(request is null)
                return ApiResults.BadRequest("INVALID_REQUEST", "Request body is required");

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return ApiResults.ValidationError(validationResult.ToDictionary());

            try
            {
                var result = await auth.RegisterAsync(request);
                return ApiResults.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResults.BadRequest("REGISTRATION_FAILED", ex.Message);
            }
        })
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithTags("v1", "Auth")
        .WithName("RegisterUser");

        // LOGIN
        app.MapPost("/auth/login", async (LoginRequest request, IValidator<LoginRequest> validator, IAuthService auth) =>
        {
            if (request is null) 
                return ApiResults.BadRequest("INVALID_REQUEST", "Request body is required");

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return ApiResults.ValidationError(validationResult.ToDictionary());

            try
            {
                var result = await auth.LoginAsync(request);
                return ApiResults.Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return ApiResults.Unauthorized("Invalid email or password");
            }
        })
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithTags("v1", "Auth")
        .WithName("LoginUser");

        // REFRESH TOKEN
        app.MapPost("/auth/refresh", async (RefreshRequest request, IValidator<RefreshRequest> validator, IAuthService auth) =>
        {
            if (request is null)
                return ApiResults.BadRequest("INVALID_REQUEST", "Request body is required");

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return ApiResults.ValidationError(validationResult.ToDictionary());

            try
            {
                var result = await auth.RefreshAsync(request.RefreshToken);
                return ApiResults.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResults.Unauthorized("Invalid or expired refresh token");
            }
        })
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithTags("v1", "Auth")
        .WithName("RefreshToken");

        // LOGOUT
        app.MapPost("/auth/logout", async (RefreshRequest request, IValidator<RefreshRequest> validator, IAuthService auth) =>
        {
            if (request is null)
                return ApiResults.BadRequest("INVALID_REQUEST", "Request body is required");

            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            await auth.LogoutAsync(request.RefreshToken);
            return ApiResults.NoContent();

        })
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithTags("v1", "Auth")
        .WithName("LogoutUser");

        // GET CURRENT USER
        app.MapGet("/me", async (HttpContext http, IAuthService auth) =>
        {
            var userId = http.User.GetUserId();
            var me = await auth.GetCurrentUserAsync(userId);

            return ApiResults.Ok(me);
        })
        .RequireAuthorization()
        .WithTags("v1", "Auth")
        .WithName("GetCurrentUser");
    }
}