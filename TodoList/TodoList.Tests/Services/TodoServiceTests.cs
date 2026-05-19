using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TodoList.Models;
using TodoList.Services;

namespace TodoList.Tests.Services;

public class TodoServiceTests
{
    private static TodoService Create() => new(NullLogger<TodoService>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_WhenEmpty_ReturnsEmptyList()
    {
        var svc = Create();
        svc.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_WhenItemsExist_ReturnsAllItems()
    {
        var svc = Create();
        svc.Add("A", null, null);
        svc.Add("B", null, null);
        svc.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void GetAll_ReturnsDefensiveCopy()
    {
        var svc = Create();
        svc.Add("A", null, null);
        var list = (List<TodoItem>)svc.GetAll();
        list.Clear();
        svc.GetAll().Should().HaveCount(1);
    }

    // ── GetByStatus ───────────────────────────────────────────────────────────

    [Fact]
    public void GetByStatus_Null_ReturnsAll()
    {
        var svc = Create();
        svc.Add("Active1", null, null);
        svc.Add("Active2", null, null);
        var completed = svc.Add("Completed", null, null);
        svc.ToggleComplete(completed.Id);

        svc.GetByStatus(null).Should().HaveCount(3);
    }

    [Fact]
    public void GetByStatus_False_ReturnsOnlyActive()
    {
        var svc = Create();
        svc.Add("Active1", null, null);
        svc.Add("Active2", null, null);
        var completed = svc.Add("Completed", null, null);
        svc.ToggleComplete(completed.Id);

        svc.GetByStatus(false).Should().HaveCount(2)
            .And.OnlyContain(i => !i.IsCompleted);
    }

    [Fact]
    public void GetByStatus_True_ReturnsOnlyCompleted()
    {
        var svc = Create();
        svc.Add("Active", null, null);
        var completed = svc.Add("Completed", null, null);
        svc.ToggleComplete(completed.Id);

        svc.GetByStatus(true).Should().HaveCount(1)
            .And.OnlyContain(i => i.IsCompleted);
    }

    [Fact]
    public void GetByStatus_NoMatch_ReturnsEmpty()
    {
        var svc = Create();
        svc.Add("Active", null, null);

        svc.GetByStatus(true).Should().BeEmpty();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public void GetById_ExistingId_ReturnsItem()
    {
        var svc = Create();
        var item = svc.Add("Test", null, null);

        svc.GetById(item.Id).Should().NotBeNull()
            .And.Subject.As<TodoItem>().Id.Should().Be(item.Id);
    }

    [Fact]
    public void GetById_NonExistentId_ReturnsNull()
    {
        var svc = Create();
        svc.GetById(999).Should().BeNull();
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_ReturnsItemWithAssignedId()
    {
        var svc = Create();
        var item = svc.Add("Test", null, null);
        item.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Add_IdIsAutoIncremented()
    {
        var svc = Create();
        var first  = svc.Add("First", null, null);
        var second = svc.Add("Second", null, null);
        second.Id.Should().BeGreaterThan(first.Id);
    }

    [Fact]
    public void Add_IsCompletedDefaultsFalse()
    {
        var svc = Create();
        svc.Add("Test", null, null).IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Add_SetsCreatedAt()
    {
        var svc = Create();
        svc.Add("Test", null, null).CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Add_UpdatedAtEqualsCreatedAt()
    {
        var svc = Create();
        var item = svc.Add("Test", null, null);
        item.UpdatedAt.Should().Be(item.CreatedAt);
    }

    [Fact]
    public void Add_ItemAppearsInGetAll()
    {
        var svc = Create();
        var item = svc.Add("Test", null, null);
        svc.GetAll().Should().ContainSingle(i => i.Id == item.Id);
    }

    [Fact]
    public void Add_NullableFieldsAcceptNull()
    {
        var svc = Create();
        var item = svc.Add("Test", null, null);
        item.Description.Should().BeNull();
        item.DueDate.Should().BeNull();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ExistingId_ReturnsUpdatedItem()
    {
        var svc  = Create();
        var item = svc.Add("Old", null, null);

        var updated = svc.Update(item.Id, "New", "Desc", null, false);

        updated.Should().NotBeNull();
        updated!.Title.Should().Be("New");
        updated.Description.Should().Be("Desc");
    }

    [Fact]
    public void Update_NonExistentId_ReturnsNull()
    {
        var svc = Create();
        svc.Update(999, "X", null, null, false).Should().BeNull();
    }

    [Fact]
    public void Update_DoesNotChangeCreatedAt()
    {
        var svc     = Create();
        var item    = svc.Add("Old", null, null);
        var created = item.CreatedAt;

        svc.Update(item.Id, "New", null, null, false);

        svc.GetById(item.Id)!.CreatedAt.Should().Be(created);
    }

    [Fact]
    public void Update_DoesNotChangeId()
    {
        var svc  = Create();
        var item = svc.Add("Old", null, null);

        svc.Update(item.Id, "New", null, null, false);

        svc.GetById(item.Id)!.Id.Should().Be(item.Id);
    }

    [Fact]
    public void Update_UpdatedAtIsRefreshed()
    {
        var svc  = Create();
        var item = svc.Add("Old", null, null);

        svc.Update(item.Id, "New", null, null, false);

        svc.GetById(item.Id)!.UpdatedAt.Should().BeOnOrAfter(item.CreatedAt);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingId_ReturnsTrue()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.Delete(item.Id).Should().BeTrue();
    }

    [Fact]
    public void Delete_NonExistentId_ReturnsFalse()
    {
        var svc = Create();
        svc.Delete(999).Should().BeFalse();
    }

    [Fact]
    public void Delete_ItemRemovedFromGetAll()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.Delete(item.Id);
        svc.GetAll().Should().NotContain(i => i.Id == item.Id);
    }

    // ── ToggleComplete ────────────────────────────────────────────────────────

    [Fact]
    public void ToggleComplete_FalseToTrue_ReturnsTrue()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.ToggleComplete(item.Id).Should().BeTrue();
        svc.GetById(item.Id)!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ToggleComplete_TrueToFalse_Toggles()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.ToggleComplete(item.Id);
        svc.ToggleComplete(item.Id);
        svc.GetById(item.Id)!.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void ToggleComplete_NonExistentId_ReturnsFalse()
    {
        var svc = Create();
        svc.ToggleComplete(999).Should().BeFalse();
    }

    [Fact]
    public void ToggleComplete_UpdatesUpdatedAt()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.ToggleComplete(item.Id);
        svc.GetById(item.Id)!.UpdatedAt.Should().BeOnOrAfter(item.CreatedAt);
    }

    // ── CountActive ───────────────────────────────────────────────────────────

    [Fact]
    public void CountActive_WhenEmpty_ReturnsZero()
    {
        Create().CountActive().Should().Be(0);
    }

    [Fact]
    public void CountActive_ReturnsOnlyActiveCount()
    {
        var svc = Create();
        svc.Add("A1", null, null);
        svc.Add("A2", null, null);
        var completed = svc.Add("C", null, null);
        svc.ToggleComplete(completed.Id);

        svc.CountActive().Should().Be(2);
    }

    [Fact]
    public void CountActive_DecreasesAfterToggleToComplete()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        var before = svc.CountActive();
        svc.ToggleComplete(item.Id);
        svc.CountActive().Should().Be(before - 1);
    }

    [Fact]
    public void CountActive_IncreasesAfterToggleToActive()
    {
        var svc  = Create();
        var item = svc.Add("Test", null, null);
        svc.ToggleComplete(item.Id);
        var before = svc.CountActive();
        svc.ToggleComplete(item.Id);
        svc.CountActive().Should().Be(before + 1);
    }
}
