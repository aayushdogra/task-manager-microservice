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
        // Example structure:

        // GET /tasks - list all tasks
        app.MapGet("/tasks", (ITaskService tasks) =>
        {
            var all = tasks.GetAll();
            return Results.Ok(all);
        })
        .WithName("GetTasks");

        // GET /tasks/{id} - get one task
        app.MapGet("/tasks/{id:int}", (int id, ITaskService tasks) =>
        {
            var task = tasks.GetById(id);
            return task is not null ? Results.Ok(task) : Results.NotFound();
        })
        .WithName("GetTaskById");

        // POST /tasks - create
        app.MapPost("/tasks", (CreateTaskRequest request, ITaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            var created = tasks.Create(request.Title, request.Description);
            return Results.Created($"/tasks/{created.Id}", created);
        })
        .WithName("CreateTask");

        return app;
    }

    public record CreateTaskRequest(string Title, string? Description);
}