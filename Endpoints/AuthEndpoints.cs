using TaskManager.Dto.Auth;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", (RegisterRequest request, IAuthService auth) =>
        {
            auth.Register(request);
            return Results.Ok();
        });

        app.MapPost("/auth/login", (LoginRequest request, IAuthService auth) =>
        {
            var result = auth.Login(request);
            return Results.Ok(result);
        });
    }
}