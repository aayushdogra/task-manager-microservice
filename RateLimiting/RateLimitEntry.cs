namespace TaskManager.RateLimiting;

public class RateLimitEntry // Represents one client's rate limit state.
{
    public int PermitCount { get; set; }
    public DateTime WindowStart { get; set; }
}