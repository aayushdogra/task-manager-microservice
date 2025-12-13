using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Helpers;
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


        app.MapGet("/debug/tasks", ([FromQuery] int? take, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromServices] DbTaskService dbTasks) =>
        {
            int limit = take.GetValueOrDefault(5);
            if (limit <= 0) limit = 5;

            // Default sort
            TaskSortBy parsedSortBy = TaskSortBy.CreatedAt;
            SortDirection parsedSortDir = SortDirection.Desc;

            Enum.TryParse(sortBy, true, out parsedSortBy);
            Enum.TryParse(sortDir, true, out parsedSortDir);

            var sorted = TaskSortingHelper.ApplySorting(
                dbTasks.GetAll(),
                parsedSortBy,
                parsedSortDir
            );

            var top = sorted.Take(limit).ToList();

            return Results.Ok(top);
        })
        .WithName("Debug_GetTopTasks");

        return app;
    }
}