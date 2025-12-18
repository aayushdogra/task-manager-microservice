namespace TaskManager.RateLimiting;

public class  RateLimitOptions
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);
}