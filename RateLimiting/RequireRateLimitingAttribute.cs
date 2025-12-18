namespace TaskManager.RateLimiting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRateLimitingAttribute : Attribute {}