using TaskManager.Data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Services;

public class DbTaskService(TasksDbContext db, ILogger<DbTaskService> logger) : ITaskService
{
    private readonly ILogger<DbTaskService> _logger = logger;
    private readonly TasksDbContext _db = db;

    public IQueryable<TaskItem> GetAll()
    {
        return _db.Tasks.AsQueryable();
    }

    public TaskItem? GetById(int id)
    {
        return _db.Tasks.AsNoTracking().FirstOrDefault(t => t.Id == id);
    }

    public TaskItem Create(string title, string? description)
    {
        try
        {
            var now = DateTime.UtcNow;

            var entity = new TaskItem(
                Id: 0,
                Title: title,
                Description: description,
                IsCompleted: false,
                CreatedAt: now,
                UpdatedAt: now
            );

            _db.Tasks.Add(entity);
            _db.SaveChanges();

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task with title {Title}", title);
            throw; // Let global handler return 500
        }
    }

    public TaskItem? Update(int id, string title, string? description, bool isCompleted)
    {
        try
        {
            var task = _db.Tasks.FirstOrDefault(task => task.Id == id);
            if (task == null) return null;

            var updatedTask = task with
            {
                Title = title,
                Description = description,
                IsCompleted = isCompleted,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Entry(task).CurrentValues.SetValues(updatedTask);
            _db.SaveChanges();

            return updatedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            throw;
        }
    }

    public bool Delete(int id)
    {
        try
        {
            var task = _db.Tasks.FirstOrDefault(task => task.Id == id);
            if (task == null) return false;

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
}