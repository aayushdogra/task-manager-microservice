namespace TaskManager.Dto;

public record UpdateTaskRequest(string Title, string? Description, bool IsCompleted);