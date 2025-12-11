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
            const TaskSortBy defaultSortBy = TaskSortBy.CreatedAt;
            const SortDirection defaultSortDir = SortDirection.Desc;

            // Parse sortBy (case-insensitive). If parsing fails, fallback to default.
            TaskSortBy currentSortBy = defaultSortBy;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<TaskSortBy>(sortBy.Trim(), true, out var parsedSortBy))
                {
                    // Friendly fallback: log and use default
                    Log.Warning("Invalid sortBy value '{SortBy}', using default '{Default}'", sortBy, defaultSortBy);
                    currentSortBy = defaultSortBy;
                }
                else currentSortBy = parsedSortBy;
            }

            // Parse sortDir
            SortDirection currentDir = defaultSortDir;
            if (!string.IsNullOrWhiteSpace(sortDir))
            {
                if (!Enum.TryParse<SortDirection>(sortDir.Trim(), true, out var parsedDir))
                {
                    Log.Warning("Invalid sortDir value '{SortDir}', using default '{Default}'", sortDir, defaultSortDir);
                    currentDir = defaultSortDir;
                }
                else currentDir = parsedDir;
            }

            const int defaultPageNumber = 1;
            const int defaultPageSize = 10;
            const int maxPageSize = 50;

            int currentPageNumber = page.GetValueOrDefault(defaultPageNumber);
            int currentPageSize = pageSize.GetValueOrDefault(defaultPageSize);

            if(currentPageNumber <= 0) currentPageNumber = defaultPageNumber;
            if(currentPageSize <= 0) currentPageSize = defaultPageSize;
            if(currentPageSize > maxPageSize) currentPageSize = maxPageSize;

            var query = tasks.GetAll();

            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);

            IQueryable<TaskItem> sorted = (currentSortBy, currentDir) switch
            {
                (TaskSortBy.Title, SortDirection.Asc) => query.OrderBy(t => t.Title),
                (TaskSortBy.Title, SortDirection.Desc) => query.OrderByDescending(t => t.Title),
                (TaskSortBy.UpdatedAt, SortDirection.Asc) => query.OrderBy(t => t.UpdatedAt),
                (TaskSortBy.UpdatedAt, SortDirection.Desc) => query.OrderByDescending(t => t.UpdatedAt),
                (TaskSortBy.CreatedAt, SortDirection.Asc) => query.OrderBy(t => t.CreatedAt),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            // Get total count before pagination and after filtering
            var totalCount = sorted.Count();

            // Calculate total pages
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)currentPageSize);

            // Clamping logic
            int pageToUse;

            if (totalPages == 0) pageToUse = 1;
            else
            {
                pageToUse = currentPageNumber;
                if (pageToUse < 1) pageToUse = 1;
                if (pageToUse > totalPages) pageToUse = totalPages;
            }

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