using TaskManager.Dto;
using TaskManager.Helpers;
using TaskManager.Models;

namespace TaskManager.Services;

public class InMemoryTaskService : ITaskService
{
    private readonly List<TaskItem> _tasks = [];
    private int _nextId = 1;

    public IQueryable<TaskItem> GetAll() => _tasks.AsQueryable();

    public TaskItem? GetById(int id) => _tasks.FirstOrDefault(t => t.Id == id);

    public TaskItem Create(string title, string? description)
    {
        var now = DateTime.UtcNow;

        var task = new TaskItem(_nextId++, title, description, false, now, now);
        _tasks.Add(task);
        return task;
    }

    public TaskItem? Update(int id, string title, string? description, bool isCompleted)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == id);
        if (existing is null) return null;

        var updated = existing with 
        { 
            Title = title, 
            Description = description, 
            IsCompleted = isCompleted,
            UpdatedAt = DateTime.UtcNow
        };

        _tasks[_tasks.IndexOf(existing)] = updated;
        return updated;
    }

    public bool Delete(int id)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == id);
        if (existing is null) return false;
        _tasks.Remove(existing);
        return true;
    }

    public PagedResponse<TaskResponse> GetTasks(bool? isCompleted, int page, int pageSize, TaskSortBy sortBy, SortDirection sortDir)
    {
        IQueryable<TaskItem> query = _tasks.AsQueryable();

        // Filtering
        if (isCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == isCompleted.Value);

        // Sorting
        query = TaskSortingHelper.ApplySorting(query, sortBy, sortDir);

        // Total count
        var totalCount = query.Count();

        // Page clamping
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var pageToUse = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

        // Pagination
        var items = query
            .Skip((pageToUse - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskResponse(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToList();

        return new PagedResponse<TaskResponse>(
            items,
            pageToUse,
            pageSize,
            totalCount
        );
    }
}