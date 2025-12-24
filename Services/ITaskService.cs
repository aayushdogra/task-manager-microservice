using TaskManager.Models;
using TaskManager.Dto;

namespace TaskManager.Services;

public interface ITaskService
{
    IQueryable<TaskItem> GetAll();
    TaskItem? GetById(Guid userId, int id);
    TaskItem Create(Guid userId, string title, string? description);
    TaskItem? Update(Guid userId, int id, string title, string? description, bool isCompleted);
    bool Delete(Guid userId, int id);
    Task<PagedResponse<TaskResponse>> GetTasksAsync(Guid userId, bool? isCompleted, int page, int pageSize, TaskSortBy sortBy, SortDirection sortDir);
}