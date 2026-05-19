using TodoList.Models;

namespace TodoList.ViewModels;

public class TodoListViewModel
{
    public IReadOnlyList<TodoItem> Items { get; set; } = [];
    public string Filter { get; set; } = "all";
    public int ActiveCount { get; set; }
}
