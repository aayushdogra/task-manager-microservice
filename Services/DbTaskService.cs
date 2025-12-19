using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Helpers;
using TaskManager.Models;

namespace TaskManager.Services;

public class DbTaskService(TasksDbContext db, ILogger<DbTaskService> logger) : ITaskService
{
    private readonly ILogger<DbTaskService> _logger = logger;
    private readonly TasksDbContext _db = db;

    public IQueryable<TaskItem> GetAll()
    {
        return _db.Tasks.AsQueryable();
    }

    public TaskItem? GetById(Guid userId, int id)
    {
        return _db.Tasks.AsNoTracking().FirstOrDefault(t => t.Id == id && t.UserId == userId);
    }

    public TaskItem Create(Guid userId, string title, string? description)
    {
        try
        {
            var now = DateTime.UtcNow;

            var entity = new TaskItem
            {
                Title = title,
                Description = description,
                IsCompleted = false,
                CreatedAt = now,
                UpdatedAt = now,
                UserId = userId
            };

            _db.Tasks.Add(entity);
            _db.SaveChanges();

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task with title {Title}", title);
            throw;
        }
    }

    public TaskItem? Update(Guid userId, int id, string title, string? description, bool isCompleted)
    {
        try
        {
            var task = _db.Tasks.FirstOrDefault(task => task.Id == id && task.UserId == userId);
            if (task is null) return null;

            task.Title = title;
            task.Description = description;
            task.IsCompleted = isCompleted;
            task.UpdatedAt = DateTime.UtcNow;
            
            _db.SaveChanges();

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            throw;
        }
    }

    public bool Delete(Guid userId, int id)
    {
        try
        {
            var task = _db.Tasks.FirstOrDefault(task => task.Id == id && task.UserId == userId);
            if (task is null) return false;

            _db.Tasks.Remove(task);
            _db.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            throw;
        }
    }

    public PagedResponse<TaskResponse> GetTasks(Guid userId, bool? isCompleted, int page, int pageSize, TaskSortBy sortBy, SortDirection sortDir)
    {
        try
        {
            IQueryable<TaskItem> query = _db.Tasks.AsNoTracking().Where(task => task.UserId == userId);

            // Filtering
            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);
            
            // Sorting
            query = TaskSortingHelper.ApplySorting(query, sortBy, sortDir);

            // Total count before pagination
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged tasks");
            throw;
        }
    }
}