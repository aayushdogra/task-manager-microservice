using TaskManager.Models;
using TaskManager.Dto;

namespace TaskManager.Services;

public interface ITaskService
{
    IQueryable<TaskItem> GetAll();
    TaskItem? GetById(int id);
    TaskItem Create(string title, string? description);
    TaskItem? Update(int id, string title, string? description, bool isCompleted);
    bool Delete(int id);
    PagedResponse<TaskResponse> GetTasks(bool? isCompleted, int page, int pageSize, TaskSortBy sortBy, SortDirection sortDir);
}