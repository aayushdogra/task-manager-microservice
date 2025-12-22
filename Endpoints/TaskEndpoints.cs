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
        /// <summary>
        /// GET /tasks
        /// Returns a paginated + sorted + filterable list of tasks.
        /// Query Params:
        /// - isCompleted (bool?) : filter by completion
        /// - page (int?) : 1-based page number
        /// - pageSize (int?) : size per page (max 50)
        /// - sortBy (string?) : CreatedAt | UpdatedAt | Title
        /// - sortDir (string?) : Asc | Desc
        /// 
        /// Response:
        /// {
        ///   items: TaskResponse[],
        ///   page: number,
        ///   pageSize: number,
        ///   totalCount: number,
        ///   totalPages: number,
        ///   hasNextPage: bool,
        ///   hasPreviousPage: bool
        /// }
        /// </summary>

        // GET /tasks - paginated, filter by isCompleted, sorted by CreatedAt desc
        app.MapGet("/tasks", (bool? isCompleted, int? page, int? pageSize, string? sortBy, string? sortDir, HttpContext http, ITaskService tasks) =>
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

            // Normalize pagination (defaults + limits)
            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

            // Fetch paginated, filtered, sorted tasks
            var response = tasks.GetTasks(userId, isCompleted, normalizedPage, normalizedPageSize, currentSortBy, currentDir);

            // Read cache info set by service
            var isCacheHit = http.Items["CacheHit"] as bool? == true;
            http.Response.Headers["X-Cache"] = isCacheHit ? "HIT" : "MISS";

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("GetTasks");

        // GET /tasks/{id} - get single task by id
        app.MapGet("/tasks/{id:int}", (int id, HttpContext http, ITaskService tasks) =>
        {
            var userId = http.User.GetUserId();
            var task = tasks.GetById(userId, id);

            if (task is null) return Results.NotFound();

            var response = new TaskResponse(
                task.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAt,
                task.UpdatedAt
            );

            return Results.Ok(response); 
        })
        .RequireAuthorization()
        .WithName("GetTaskById");

        // POST /tasks - create new task
        app.MapPost("/tasks", async(CreateTaskRequest request, IValidator<CreateTaskRequest> validator, HttpContext http, ITaskService tasks) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

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

            return Results.Created($"/tasks/{created.Id}", response);
        })
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithName("CreateTask");

        // PUT /tasks/{id} - update existing task
        app.MapPut("/tasks/{id:int}", async(int id, UpdateTaskRequest request, IValidator<UpdateTaskRequest> validator, HttpContext http, ITaskService tasks) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = http.User.GetUserId();
            var updated = tasks.Update(userId, id, request.Title, request.Description, request.IsCompleted);
            
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
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithName("UpdateTask");

        // DELETE /tasks/{id} - delete task by id
        app.MapDelete("/tasks/{id:int}", (int id, HttpContext http, ITaskService tasks) =>
        {
            var userId = http.User.GetUserId();
            var deleted = tasks.Delete(userId, id);

            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithMetadata(new RequireRateLimitingAttribute())
        .WithMetadata(new RequireUserRateLimitingAttribute())
        .WithName("DeleteTask");

        return app;
    }
}