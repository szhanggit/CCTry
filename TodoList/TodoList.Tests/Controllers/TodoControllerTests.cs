using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoList.Controllers;
using TodoList.Models;
using TodoList.Services;
using TodoList.ViewModels;

namespace TodoList.Tests.Controllers;

public class TodoControllerTests
{
    private readonly Mock<ITodoService> _mockService;
    private readonly TodoController     _controller;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TodoControllerTests()
    {
        _mockService = new Mock<ITodoService>();
        _controller  = new TodoController(_mockService.Object, NullLogger<TodoController>.Instance);
    }

    private static JsonElement Json(object? value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value, _jsonOpts)).RootElement;

    // ── Index ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Index_FilterAll_CallsGetByStatusWithNull()
    {
        _mockService.Setup(s => s.GetByStatus(null)).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        _controller.Index("all");

        _mockService.Verify(s => s.GetByStatus(null), Times.Once);
    }

    [Fact]
    public void Index_FilterActive_CallsGetByStatusFalse()
    {
        _mockService.Setup(s => s.GetByStatus(false)).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        _controller.Index("active");

        _mockService.Verify(s => s.GetByStatus(false), Times.Once);
    }

    [Fact]
    public void Index_FilterCompleted_CallsGetByStatusTrue()
    {
        _mockService.Setup(s => s.GetByStatus(true)).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        _controller.Index("completed");

        _mockService.Verify(s => s.GetByStatus(true), Times.Once);
    }

    [Fact]
    public void Index_AlwaysCallsCountActive()
    {
        _mockService.Setup(s => s.GetByStatus(It.IsAny<bool?>())).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        _controller.Index("all");

        _mockService.Verify(s => s.CountActive(), Times.Once);
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        _mockService.Setup(s => s.GetByStatus(It.IsAny<bool?>())).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        var result = _controller.Index("all");

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Index_ViewModelItemsMatchService()
    {
        var items = new List<TodoItem>
        {
            new() { Id = 1, Title = "A" },
            new() { Id = 2, Title = "B" }
        };
        _mockService.Setup(s => s.GetByStatus(null)).Returns(items);
        _mockService.Setup(s => s.CountActive()).Returns(2);

        var view = (ViewResult)_controller.Index("all");
        var vm   = (TodoListViewModel)view.Model!;

        vm.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Index_ViewModelFilterMatchesInput()
    {
        _mockService.Setup(s => s.GetByStatus(false)).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(0);

        var view = (ViewResult)_controller.Index("active");
        var vm   = (TodoListViewModel)view.Model!;

        vm.Filter.Should().Be("active");
    }

    [Fact]
    public void Index_ViewModelActiveCountMatchesService()
    {
        _mockService.Setup(s => s.GetByStatus(null)).Returns(new List<TodoItem>());
        _mockService.Setup(s => s.CountActive()).Returns(5);

        var view = (ViewResult)_controller.Index("all");
        var vm   = (TodoListViewModel)view.Model!;

        vm.ActiveCount.Should().Be(5);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_InvalidModel_ReturnsSuccessFalse()
    {
        _controller.ModelState.AddModelError("Title", "Required");
        var model = new TodoFormViewModel { Title = "" };

        var result = Json(((JsonResult)_controller.Create(model)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Create_InvalidModel_DoesNotCallService()
    {
        _controller.ModelState.AddModelError("Title", "Required");
        _controller.Create(new TodoFormViewModel { Title = "" });

        _mockService.Verify(s => s.Add(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public void Create_ValidModel_CallsServiceAdd()
    {
        var model = new TodoFormViewModel { Title = "Test" };
        _mockService.Setup(s => s.Add("Test", null, null))
                    .Returns(new TodoItem { Id = 1, Title = "Test" });

        _controller.Create(model);

        _mockService.Verify(s => s.Add("Test", null, null), Times.Once);
    }

    [Fact]
    public void Create_ValidModel_ReturnsSuccessTrue()
    {
        var model = new TodoFormViewModel { Title = "Test" };
        _mockService.Setup(s => s.Add(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
                    .Returns(new TodoItem { Id = 1, Title = "Test" });

        var result = Json(((JsonResult)_controller.Create(model)).Value);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Create_ValidModel_ReturnsItemFromService()
    {
        var model = new TodoFormViewModel { Title = "Test" };
        _mockService.Setup(s => s.Add(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
                    .Returns(new TodoItem { Id = 7, Title = "Test" });

        var result = Json(((JsonResult)_controller.Create(model)).Value);

        result.GetProperty("item").GetProperty("id").GetInt32().Should().Be(7);
    }

    // ── Edit GET ──────────────────────────────────────────────────────────────

    [Fact]
    public void EditGet_ExistingId_ReturnsSuccessTrue()
    {
        _mockService.Setup(s => s.GetById(1))
                    .Returns(new TodoItem { Id = 1, Title = "Test" });

        var result = Json(((JsonResult)_controller.Edit(1)).Value);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.TryGetProperty("item", out _).Should().BeTrue();
    }

    [Fact]
    public void EditGet_NonExistentId_ReturnsSuccessFalse()
    {
        _mockService.Setup(s => s.GetById(999)).Returns((TodoItem?)null);

        var result = Json(((JsonResult)_controller.Edit(999)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ── Edit POST ─────────────────────────────────────────────────────────────

    [Fact]
    public void EditPost_InvalidModel_ReturnsSuccessFalse()
    {
        _controller.ModelState.AddModelError("Title", "Required");
        var model = new TodoFormViewModel { Title = "" };

        var result = Json(((JsonResult)_controller.Edit(1, model)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void EditPost_InvalidModel_DoesNotCallService()
    {
        _controller.ModelState.AddModelError("Title", "Required");
        _controller.Edit(1, new TodoFormViewModel { Title = "" });

        _mockService.Verify(s => s.Update(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void EditPost_ValidModel_CallsServiceUpdate()
    {
        var model = new TodoFormViewModel { Title = "Updated" };
        _mockService.Setup(s => s.Update(1, "Updated", null, null, false))
                    .Returns(new TodoItem { Id = 1, Title = "Updated" });

        _controller.Edit(1, model);

        _mockService.Verify(s => s.Update(1, "Updated", null, null, false), Times.Once);
    }

    [Fact]
    public void EditPost_ValidModel_ReturnsSuccessTrue()
    {
        var model = new TodoFormViewModel { Title = "Updated" };
        _mockService.Setup(s => s.Update(It.IsAny<int>(), It.IsAny<string>(),
                           It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
                    .Returns(new TodoItem { Id = 1, Title = "Updated" });

        var result = Json(((JsonResult)_controller.Edit(1, model)).Value);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void EditPost_NotFound_ReturnsSuccessFalse()
    {
        var model = new TodoFormViewModel { Title = "Updated" };
        _mockService.Setup(s => s.Update(It.IsAny<int>(), It.IsAny<string>(),
                           It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()))
                    .Returns((TodoItem?)null);

        var result = Json(((JsonResult)_controller.Edit(999, model)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingId_ReturnsSuccessTrue()
    {
        _mockService.Setup(s => s.Delete(1)).Returns(true);

        var result = Json(((JsonResult)_controller.Delete(1)).Value);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Delete_NonExistentId_ReturnsSuccessFalse()
    {
        _mockService.Setup(s => s.Delete(999)).Returns(false);

        var result = Json(((JsonResult)_controller.Delete(999)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
        result.TryGetProperty("message", out _).Should().BeTrue();
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Fact]
    public void Toggle_ExistingId_ReturnsSuccessTrueAndNewState()
    {
        _mockService.Setup(s => s.ToggleComplete(1)).Returns(true);
        _mockService.Setup(s => s.GetById(1))
                    .Returns(new TodoItem { Id = 1, IsCompleted = true });

        var result = Json(((JsonResult)_controller.Toggle(1)).Value);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("isCompleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Toggle_NonExistentId_ReturnsSuccessFalse()
    {
        _mockService.Setup(s => s.ToggleComplete(999)).Returns(false);

        var result = Json(((JsonResult)_controller.Toggle(999)).Value);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
    }
}
