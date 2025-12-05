using Microsoft.EntityFrameworkCore;
using TaskManager.Data;

namespace TaskManager.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new { status = "ok" });
        })
        .WithName("HealthCheck");

        app.MapGet("/db-health", async (TasksDbContext db) =>
        {
            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                return canConnect
                    ? Results.Ok(new { status = "ok" })
                    : Results.Problem("Database not reachable");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        })
        .WithName("DatabaseHealthCheck");

        app.MapGet("/db-tasks-count", async (TasksDbContext db) =>
        {
            var count = await db.Tasks.CountAsync();
            return Results.Ok(new { tasksInDb = count });
        })
        .WithName("DatabaseTasksCount");

        return app;
    }
}