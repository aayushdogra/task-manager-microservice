using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Helpers;
using TaskManager.Services;
using StackExchange.Redis;

namespace TaskManager.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new 
            { 
                status = "ok",
                message = "Task Manager Microservice is running"
            });
        })
        .WithTags("v1", "Health")
        .WithName("HealthCheck");

        app.MapGet("/db-health", async (TasksDbContext db) =>
        {
            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                return canConnect
                    ? Results.Ok(new 
                    { 
                        status = "ok",
                        message = "Database connection successful"
                    })
                    : Results.Problem("Database not reachable");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        })
        .WithTags("v1", "Health")
        .WithName("DatabaseHealthCheck");

        app.MapGet("/redis-health", (IConnectionMultiplexer redis) =>
        {
            try
            {
                var redisDB = redis.GetDatabase();
                redisDB.StringSet("health_check", "ok");
                var value = redisDB.StringGet("health_check");

                return value == "ok" ? Results.Ok(new 
                { 
                    status = "ok",
                    message = "Redis connection successful"
                })
                    : Results.Problem("Redis not responding correctly");
            }
            catch(Exception ex)
            {
                return Results.Problem($"Redis error: {ex.Message}");
            }
        })
        .WithTags("v1", "Health")
        .WithName("RedisHealthCheck");

        // Debug endpoint to get count of tasks in the database
        app.MapGet("/db-tasks-count", async (TasksDbContext db) =>
        {
            var count = await db.Tasks.CountAsync();
            return Results.Ok(new { tasksInDb = count });
        })
        .WithTags("v1", "Health")
        .WithName("DatabaseTasksCount");

        // Debug endpoint to test create tasks in the database
        app.MapPost("/db-test-task", ([FromQuery] string title, [FromQuery] string? description, HttpContext http, [FromServices] DbTaskService dbTasks) =>
        {
            var userId = http.User.GetUserId();
            var created = dbTasks.Create(userId, title, description);
            return Results.Ok(created);
        })
        .RequireAuthorization()
        .WithTags("v1", "Health")
        .WithName("DbTestCreateTask");

        // Debug endpoint to get top N tasks with sorting options
        app.MapGet("/debug/tasks", ([FromQuery] int? take, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromServices] DbTaskService dbTasks) =>
        {
            int limit = take.GetValueOrDefault(5);
            if (limit <= 0) limit = 5;

            // Default sort
            TaskSortBy parsedSortBy = TaskSortBy.CreatedAt;
            SortDirection parsedSortDir = SortDirection.Desc;

            Enum.TryParse(sortBy, true, out parsedSortBy);
            Enum.TryParse(sortDir, true, out parsedSortDir);

            var sorted = TaskSortingHelper.ApplySorting(dbTasks.GetAll(), parsedSortBy, parsedSortDir);

            var top = sorted.Take(limit).ToList();

            return Results.Ok(top);
        })
        .WithTags("v1", "Health")
        .WithName("Debug_GetTopTasks");

        return app;
    }
}