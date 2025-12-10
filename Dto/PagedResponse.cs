namespace TaskManager.Dto;

public record PagedResponse<T>(IEnumerable<T> Items, int PageNumber, int PageSize, int TotalCount);