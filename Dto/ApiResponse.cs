namespace TaskManager.Dto;

public record ApiResponse<T>(bool Success, T? Data, ApiError? Error, object? Meta = null);
public record ApiError(string Code, string Message, object? Details = null);