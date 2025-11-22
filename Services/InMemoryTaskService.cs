using TaskManager.Models;

namespace TaskManager.Services;

public class InMemoryTaskService : ITaskService
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public IEnumerable<TaskItem> GetAll() => _tasks;

    public TaskItem? GetById(int id) => _tasks.FirstOrDefault(t => t.Id == id);

    public TaskItem Create(string title, string? description)
    {
        var task = new TaskItem(_nextId++, title, description, false);
        _tasks.Add(task);
        return task;
    }

    public TaskItem? Update(int id, string title, string? description, bool isCompleted)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == id);
        if (existing is null) return null;

        var updated = existing with { Title = title, Description = description, IsCompleted = isCompleted };
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
}