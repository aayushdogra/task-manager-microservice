using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Data;
using TaskManager.Services;

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

        // Debug endpoint to test create tasks in the database
        app.MapPost("/db-test-task", ([FromQuery] string title, [FromQuery] string? description, [FromServices] DbTaskService dbTasks) =>
        {
            var created = dbTasks.Create(title, description);
            return Results.Ok(created);
        })
        .WithName("DbTestCreateTask");

        return app;
    }
}