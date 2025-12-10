namespace TaskManager.Dto;

public record PagedResponse<T>(
    IEnumerable<T> Items, 
    int Page,
    int PageSize, 
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);