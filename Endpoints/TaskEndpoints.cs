using Serilog;
using TaskManager.Dto;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /tasks - paginated, filter by isCompleted, sorted by CreatedAt desc
        app.MapGet("/tasks", (bool? isCompleted, int? page, int? pageSize, string? sortBy, string? sortDir, ITaskService tasks) =>
        {
            // Allowed enum names for validation
            var allowedSortBy = Enum.GetNames(typeof(TaskSortBy));
            var allowedSortDir = Enum.GetNames(typeof(SortDirection));

            // Default values
            const TaskSortBy defaultSortBy = TaskSortBy.CreatedAt;
            const SortDirection defaultSortDir = SortDirection.Desc;

            // Strict validation for sortBy
            TaskSortBy currentSortBy = defaultSortBy;

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<TaskSortBy>(sortBy.Trim(), true, out var parsedSortBy))
                {
                    return Results.BadRequest(new 
                    { 
                        error = "Invalid sortBy parameter.",
                        allowedValues = allowedSortBy
                    });
                }
                currentSortBy = parsedSortBy;
            }

            // Strict validation for sortDir
            SortDirection currentDir = defaultSortDir;

            if (!string.IsNullOrWhiteSpace(sortDir))
            {
                if (!Enum.TryParse<SortDirection>(sortDir.Trim(), true, out var parsedDir))
                {
                    return Results.BadRequest(new
                    {
                        error = "Invalid sortDir parameter.",
                        allowedValues = allowedSortDir
                    });
                }
                currentDir = parsedDir;
            }

            // Pagination defaults and limits
            const int defaultPageNumber = 1;
            const int defaultPageSize = 10;
            const int maxPageSize = 50;

            int currentPageNumber = page.GetValueOrDefault(defaultPageNumber);
            int currentPageSize = pageSize.GetValueOrDefault(defaultPageSize);

            if(currentPageNumber <= 0) currentPageNumber = defaultPageNumber;
            if(currentPageSize <= 0) currentPageSize = defaultPageSize;
            if(currentPageSize > maxPageSize) currentPageSize = maxPageSize;

            // Query source
            var query = tasks.GetAll();

            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);

            // Apply primary sorting + stable secondary sorting by (Id) with direction-aware ThenBy
            IQueryable<TaskItem> sorted = (currentSortBy, currentDir) switch
            {
                (TaskSortBy.Title, SortDirection.Asc) => 
                    query.OrderBy(t => t.Title).ThenBy(t => t.Id),
                (TaskSortBy.Title, SortDirection.Desc) => 
                    query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),
                (TaskSortBy.UpdatedAt, SortDirection.Asc) => 
                    query.OrderBy(t => t.UpdatedAt).ThenBy(t => t.Id),
                (TaskSortBy.UpdatedAt, SortDirection.Desc) => 
                    query.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id),
                (TaskSortBy.CreatedAt, SortDirection.Asc) => 
                    query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
                _ => 
                    query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            };

            // Total count after filters, before pagination
            var totalCount = sorted.Count();

            // Total pages
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)currentPageSize);

            // Clamping logic -> clamp page number within valid range
            int pageToUse;

            if (totalPages == 0) pageToUse = 1;
            else
            {
                pageToUse = currentPageNumber;
                if (pageToUse < 1) pageToUse = 1;
                if (pageToUse > totalPages) pageToUse = totalPages;
            }

            // Fetch page (use clamped pageToUse)
            var pageItems = sorted
                    .Skip((pageToUse - 1) * currentPageSize)
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
                pageToUse,
                currentPageSize,
                totalCount,
                totalPages,
                pageToUse < totalPages,
                totalPages > 0 && pageToUse > 1
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