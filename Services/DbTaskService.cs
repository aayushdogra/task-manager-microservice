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
        // TODO: implement DB-backed retrieval of all tasks
        return _db.Tasks.AsNoTracking().ToList();
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
        throw new NotImplementedException("DbTaskService.Update is not implemented yet.");
    }

    public bool Delete(int id)
    {
        throw new NotImplementedException("DbTaskService.Delete is not implemented yet.");
    }
}