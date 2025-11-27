using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /tasks - list all tasks
        // Filter by isCompleted if query parameter is provided
        app.MapGet("/tasks", (bool? isCompleted, ITaskService tasks) =>
        {
            var all = tasks.GetAll();

            if (isCompleted.HasValue)
                all = all.Where(t => t.IsCompleted == isCompleted.Value);

            return Results.Ok(all);
        })
        .WithName("GetTasks");

        // GET /tasks/{id} - get single task by id
        app.MapGet("/tasks/{id:int}", (int id, ITaskService tasks) =>
        {
            var task = tasks.GetById(id);
            return task is not null ? Results.Ok(task) : Results.NotFound();
        })
        .WithName("GetTaskById");

        // POST /tasks - create new task
        app.MapPost("/tasks", (CreateTaskRequest request, ITaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            var created = tasks.Create(request.Title, request.Description);
            return Results.Created($"/tasks/{created.Id}", created);
        })
        .WithName("CreateTask");

        // PUT /tasks/{id} - update existing task
        app.MapPut("/tasks/{id:int}", async (int id, UpdateTaskRequest request, ITaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            var updated = tasks.Update(id, request.Title, request.Description, request.IsCompleted);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        })
        .WithName("UpdateTask");

        // DELETE /tasks/{id} - delete task by id
        app.MapDelete("/tasks/{id:int}", (int id, ITaskService tasks) =>
        {
            var deleted = tasks.Delete(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteTask");

        return app;
    }

    // DTO for creating a tasks
    public record CreateTaskRequest(string Title, string? Description);
    public record UpdateTaskRequest(string Title, string? Description, bool IsCompleted);
}