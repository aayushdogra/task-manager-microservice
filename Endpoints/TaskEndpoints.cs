using FluentValidation;
using TaskManager.Dto;
using TaskManager.Services;
using TaskManager.Helpers;
using TaskManager.RateLimiting;

namespace TaskManager.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /tasks - paginated, filter by isCompleted, sorted by CreatedAt desc
        app.MapGet("/tasks", async (bool? isCompleted, int? page, int? pageSize, string? sortBy, string? sortDir, HttpContext http, ITaskService tasks) =>
        {
            var userId = http.User.GetUserId();

            // Allowed enum names for validation
            var allowedSortBy = Enum.GetNames<TaskSortBy>();
            var allowedSortDir = Enum.GetNames<SortDirection>();

            // Default values
            const TaskSortBy defaultSortBy = TaskSortBy.CreatedAt;
            const SortDirection defaultSortDir = SortDirection.Desc;

            // Strict validation for sortBy
            TaskSortBy currentSortBy = defaultSortBy;

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<TaskSortBy>(sortBy.Trim(), true, out var parsedSortBy))
                    return ApiResults.BadRequest("INVALID_SORT_BY", "Invalid sortBy parameter.", allowedSortBy);
                
                currentSortBy = parsedSortBy;
            }

            // Strict validation for sortDir
            SortDirection currentDir = defaultSortDir;

            if (!string.IsNullOrWhiteSpace(sortDir))
            {
                if (!Enum.TryParse<SortDirection>(sortDir.Trim(), true, out var parsedDir))
                    return ApiResults.BadRequest("INVALID_SORT_DIR", "Invalid sortDir parameter.", allowedSortDir);
                
                currentDir = parsedDir;
            }

            // Normalize pagination (defaults + limits)
            int normalizedPage, normalizedPageSize;
            
            try
            {
                (normalizedPage, normalizedPageSize) = PaginationHelper.ValidateAndNormalize(page, pageSize);
            }
            catch(ArgumentException ex)
            {
                return ApiResults.BadRequest("PAGINATION_ERROR", ex.Message);
            }

            // Fetch paginated, filtered, sorted tasks
            var response = await tasks.GetTasksAsync(userId, isCompleted, normalizedPage, normalizedPageSize, currentSortBy, currentDir);

            var request = http.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.Path}";

            var links = new List<string>
            {
                // Self link
                $"<{baseUrl}?page={response.Page}&pageSize={response.PageSize}>; rel=\"self\""
            };

            // Next page link
            if (response.HasNextPage)
                links.Add($"<{baseUrl}?page={response.Page + 1}&pageSize={response.PageSize}>; rel=\"next\"");

            // Previous page link
            if (response.HasPreviousPage)
                links.Add($"<{baseUrl}?page={response.Page - 1}&pageSize={response.PageSize}>; rel=\"prev\"");

            http.Response.Headers.Append("Link", string.Join(", ", links));

            // Read cache info set by service
            var isCacheHit = http.Items["CacheHit"] as bool? == true;
            http.Response.Headers["X-Cache"] = isCacheHit ? "HIT" : "MISS";

            return ApiResults.Ok(response);
        })
        .RequireAuthorization()
        .WithTags("v1", "Tasks")
        .WithName("GetTasks");

        // GET /tasks/{id} - get single task by id
        app.MapGet("/tasks/{id:int}", (int id, HttpContext http, ITaskService tasks) =>
        {
            var userId = http.User.GetUserId();
            var task = tasks.GetById(userId, id);

            if (task is null)
                return ApiResults.NotFound("TASK_NOT_FOUND", $"Task with ID {id} not found.");
            

            var response = new TaskResponse(
                task.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAt,
                task.UpdatedAt
            );

            return ApiResults.Ok(response);
        })
        .RequireAuthorization()
        .WithTags("v1", "Tasks")
        .WithName("GetTaskById");

        // POST /tasks - create new task
        app.MapPost("/tasks", async(CreateTaskRequest request, IValidator<CreateTaskRequest> validator, HttpContext http, ITaskService tasks) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return ApiResults.ValidationError(validationResult.ToDictionary());

            var userId = http.User.GetUserId();
            var created = tasks.Create(userId, request.Title, request.Description);

            var response = new TaskResponse(
                created.Id,
                created.Title,
                created.Description,
                created.IsCompleted,
                created.CreatedAt,
                created.UpdatedAt
            );

            return ApiResults.Created($"/tasks/{created.Id}", response);
        })
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithTags("v1", "Tasks")
        .WithName("CreateTask");

        // PUT /tasks/{id} - update existing task
        app.MapPut("/tasks/{id:int}", async(int id, UpdateTaskRequest request, IValidator<UpdateTaskRequest> validator, HttpContext http, ITaskService tasks) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
                return ApiResults.ValidationError(validationResult.ToDictionary());
            

            var userId = http.User.GetUserId();
            var updated = tasks.Update(userId, id, request.Title, request.Description, request.IsCompleted);
            
            if(updated is null)
                return ApiResults.NotFound("TASK_NOT_FOUND", "Task not found");
            

            var response = new TaskResponse(
                updated!.Id,
                updated.Title,
                updated.Description,
                updated.IsCompleted,
                updated.CreatedAt,
                updated.UpdatedAt
            );

            return ApiResults.Ok(response);
        })
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithTags("v1", "Tasks")
        .WithName("UpdateTask");

        // DELETE /tasks/{id} - delete task by id
        app.MapDelete("/tasks/{id:int}", (int id, HttpContext http, ITaskService tasks) =>
        {
            var userId = http.User.GetUserId();
            var deleted = tasks.Delete(userId, id);

            if (!deleted)
                return ApiResults.NotFound("TASK_NOT_FOUND", "Task not found");
            

            return ApiResults.NoContent();
        })
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithTags("v1", "Tasks")
        .WithName("DeleteTask");

        return app;
    }
}