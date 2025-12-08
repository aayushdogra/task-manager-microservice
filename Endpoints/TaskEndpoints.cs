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

            var response = all.Select(t => new TaskResponse(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.UpdatedAt
            ));

            return Results.Ok(response);
        })
        .WithName("GetTasks");

        // GET /tasks/{id} - get single task by id
        app.MapGet("/tasks/{id:int}", (int id, ITaskService tasks) =>
        {
            var task = tasks.GetById(id);

            if (task is null) Results.NotFound();

            var response = new TaskResponse(
                task!.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAt,
                task.UpdatedAt
            );

            return Results.Ok(response); 
        })
        .WithName("GetTaskById");

        // POST /tasks - create new task
        app.MapPost("/tasks", (CreateTaskRequest request, ITaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            var created = tasks.Create(request.Title, request.Description);

            var response = new TaskResponse(
                created.Id,
                created.Title,
                created.Description,
                created.IsCompleted,
                created.CreatedAt,
                created.UpdatedAt
            );

            return Results.Created($"/tasks/{created.Id}", response);
        })
        .WithName("CreateTask");

        // PUT /tasks/{id} - update existing task
        app.MapPut("/tasks/{id:int}", (int id, UpdateTaskRequest request, ITaskService tasks) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            var updated = tasks.Update(id, request.Title, request.Description, request.IsCompleted);
            
            if(updated is null)
                return Results.NotFound();

            var response = new TaskResponse(
                updated!.Id,
                updated.Title,
                updated.Description,
                updated.IsCompleted,
                updated.CreatedAt,
                updated.UpdatedAt
            );

            return Results.Ok(response);
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
}