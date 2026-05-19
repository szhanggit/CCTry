using Microsoft.AspNetCore.Mvc;
using TodoList.Services;
using TodoList.ViewModels;

namespace TodoList.Controllers;

public class TodoController : Controller
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodoController> _logger;

    public TodoController(ITodoService todoService, ILogger<TodoController> logger)
    {
        _todoService = todoService;
        _logger = logger;
    }

    public IActionResult Index(string filter = "all")
    {
        bool? statusFilter = filter switch
        {
            "active" => false,
            "completed" => true,
            _ => null
        };

        var normalizedFilter = filter is "active" or "completed" ? filter : "all";
        var items = _todoService.GetByStatus(statusFilter);
        var activeCount = _todoService.CountActive();

        ViewData["ActiveCount"] = activeCount;

        return View(new TodoListViewModel
        {
            Items = items,
            Filter = normalizedFilter,
            ActiveCount = activeCount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([FromBody] TodoFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, errors });
        }

        var item = _todoService.Add(model.Title, model.Description, model.DueDate);
        _logger.LogInformation("Created todo item {Id} via web", item.Id);

        return Json(new
        {
            success = true,
            item = new
            {
                item.Id,
                item.Title,
                item.Description,
                item.DueDate,
                item.IsCompleted,
                item.CreatedAt
            }
        });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var item = _todoService.GetById(id);
        if (item is null)
            return Json(new { success = false });

        return Json(new
        {
            success = true,
            item = new
            {
                item.Id,
                item.Title,
                item.Description,
                item.DueDate,
                item.IsCompleted
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [FromBody] TodoFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, errors });
        }

        var item = _todoService.Update(id, model.Title, model.Description, model.DueDate, model.IsCompleted);
        if (item is null)
            return Json(new { success = false });

        return Json(new
        {
            success = true,
            item = new
            {
                item.Id,
                item.Title,
                item.Description,
                item.DueDate,
                item.IsCompleted,
                item.CreatedAt
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var deleted = _todoService.Delete(id);
        if (!deleted)
            return Json(new { success = false, message = "Item not found" });

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Toggle(int id)
    {
        var toggled = _todoService.ToggleComplete(id);
        if (!toggled)
            return Json(new { success = false });

        var item = _todoService.GetById(id);
        return Json(new { success = true, isCompleted = item!.IsCompleted });
    }
}
