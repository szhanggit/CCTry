using TodoList.Models;

namespace TodoList.Services;

public interface ITodoService
{
    IReadOnlyList<TodoItem> GetAll();
    IReadOnlyList<TodoItem> GetByStatus(bool? isCompleted);
    TodoItem? GetById(int id);
    TodoItem Add(string title, string? description, DateTime? dueDate);
    TodoItem? Update(int id, string title, string? description, DateTime? dueDate, bool isCompleted);
    bool Delete(int id);
    bool ToggleComplete(int id);
    int CountActive();
}
