using TaskManager.Dto;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /tasks - paginated, filter by isCompleted, sorted by CreatedAt desc
        app.MapGet("/tasks", (bool? isCompleted, int?page, int?pageSize, ITaskService tasks) =>
        {
            const int defaultPageNumber = 1;
            const int defaultPageSize = 10;
            const int maxPageSize = 50;

            int currentPageNumber = page.GetValueOrDefault(defaultPageNumber);
            int currentPageSize = pageSize.GetValueOrDefault(defaultPageSize);

            if(currentPageNumber <= 0) currentPageNumber = defaultPageNumber;
            if(currentPageSize <= 0) currentPageSize = defaultPageSize;
            if(currentPageSize > maxPageSize) currentPageSize = maxPageSize;

            var all = tasks.GetAll();

            if (isCompleted.HasValue)
                all = all.Where(t => t.IsCompleted == isCompleted.Value);

            var sorted = all.OrderByDescending(t => t.CreatedAt);

            var totalCount = sorted.Count();

            var pageItems = sorted
                .Skip((currentPageNumber - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToList();

            var items = pageItems.Select(t => new TaskResponse(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.UpdatedAt
            ));

            var response = new PagedResponse<TaskResponse>(
                items,
                currentPageNumber,
                currentPageSize,
                totalCount
            );

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