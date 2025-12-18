using TaskManager.RateLimiting;

namespace TaskManager.Middleware;

public class RateLimitingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext httpContext, RateLimitOptions options, InMemoryRateLimitStore store)
    {
        var endpoint = httpContext.GetEndpoint();

        // Apply rate limiting only to API endpoints has metadata
        var requiresRateLimiting = endpoint?.Metadata.GetMetadata<RequireRateLimitingAttribute>() != null;

        if(!requiresRateLimiting)
        {
            await _next(httpContext);
            return;
        }

        var key = GetClientKey(httpContext);

        if(!store.TryConsume(key, options, out var remaining))
        {
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later."
            });

            return;
        }

        httpContext.Response.Headers["X-RateLimit-Limit"] = options.PermitLimit.ToString();
        httpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        await _next(httpContext);
    }

    private static string GetClientKey(HttpContext context)
    {
        // Per-IP rate limiting
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}