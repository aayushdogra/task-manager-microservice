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
        // We will fill this next:
        // - GET /tasks
        // - GET /tasks/{id}
        // - POST /tasks
        // - PUT /tasks/{id}
        // - DELETE /tasks/{id}

        return app;
    }
}