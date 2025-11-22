using TaskManager.Models;

namespace TaskManager.Services;

public interface ITaskService
{
    IEnumerable<TaskItem> GetAll();
    TaskItem? GetById(int id);
    TaskItem Create(string title, string? description);
    TaskItem? Update(int id, string title, string? description, bool isCompleted);
    bool Delete(int id);
}