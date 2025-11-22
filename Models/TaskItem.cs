namespace TaskManager.Models;

public record TaskItem(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted
);