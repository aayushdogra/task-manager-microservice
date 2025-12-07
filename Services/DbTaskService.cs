using TaskManager.Data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Services;

public class DbTaskService : ITaskService
{
    private readonly TasksDbContext _db;

    public DbTaskService(TasksDbContext db) => _db = db;

    public IEnumerable<TaskItem> GetAll()
    {
        return [.. _db.Tasks.AsNoTracking()];
    }

    public TaskItem? GetById(int id)
    {
        return _db.Tasks.AsNoTracking().FirstOrDefault(t => t.Id == id);
    }

    public TaskItem Create(string title, string? description)
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

    public TaskItem? Update(int id, string title, string? description, bool isCompleted)
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

    public bool Delete(int id)
    {
        var task = _db.Tasks.FirstOrDefault(task => task.Id == id);
        if (task == null) return false;

        _db.Tasks.Remove(task);
        _db.SaveChanges();
        return true;
    }
}