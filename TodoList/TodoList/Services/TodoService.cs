using TodoList.Models;

namespace TodoList.Services;

public class TodoService : ITodoService
{
    private readonly List<TodoItem> _items = new();
    private int _nextId = 1;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<TodoService> _logger;

    public TodoService(ILogger<TodoService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<TodoItem> GetAll()
    {
        _lock.EnterReadLock();
        try { return _items.ToList(); }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<TodoItem> GetByStatus(bool? isCompleted)
    {
        var all = GetAll();
        return isCompleted is null ? all : all.Where(i => i.IsCompleted == isCompleted).ToList();
    }

    public TodoItem? GetById(int id)
    {
        _lock.EnterReadLock();
        try { return _items.FirstOrDefault(i => i.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    public TodoItem Add(string title, string? description, DateTime? dueDate)
    {
        _lock.EnterWriteLock();
        try
        {
            var now = DateTime.UtcNow;
            var item = new TodoItem
            {
                Id = _nextId++,
                Title = title,
                Description = description,
                DueDate = dueDate,
                IsCompleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            _items.Add(item);
            _logger.LogInformation("Added todo item {Id}: {Title}", item.Id, item.Title);
            return item;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public TodoItem? Update(int id, string title, string? description, DateTime? dueDate, bool isCompleted)
    {
        _lock.EnterWriteLock();
        try
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null) return null;

            item.Title = title;
            item.Description = description;
            item.DueDate = dueDate;
            item.IsCompleted = isCompleted;
            item.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated todo item {Id}", id);
            return item;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool Delete(int id)
    {
        _lock.EnterWriteLock();
        try
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null) return false;
            _items.Remove(item);
            _logger.LogInformation("Deleted todo item {Id}", id);
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool ToggleComplete(int id)
    {
        _lock.EnterWriteLock();
        try
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null) return false;
            item.IsCompleted = !item.IsCompleted;
            item.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Toggled todo item {Id} to {IsCompleted}", id, item.IsCompleted);
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public int CountActive()
    {
        _lock.EnterReadLock();
        try { return _items.Count(i => !i.IsCompleted); }
        finally { _lock.ExitReadLock(); }
    }
}
