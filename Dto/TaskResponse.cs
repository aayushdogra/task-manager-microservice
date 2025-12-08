namespace TaskManager.Dto
{
    public record TaskResponse(int Id, string Title, string? Description, bool IsCompleted, DateTime CreatedAt, DateTime UpdatedAt);
}