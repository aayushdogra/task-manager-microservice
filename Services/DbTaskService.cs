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
        return Enumerable.Empty<TaskItem>();
    }

    public TaskItem? GetById(int id) => null;

    public TaskItem Create(string title, string? description)
    {
        throw new NotImplementedException("DbTaskService.Create is not implemented yet.");
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