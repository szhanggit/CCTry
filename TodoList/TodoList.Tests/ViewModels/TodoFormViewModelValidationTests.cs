using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using TodoList.ViewModels;

namespace TodoList.Tests.ViewModels;

public class TodoFormViewModelValidationTests
{
    private static IList<ValidationResult> Validate(TodoFormViewModel model)
    {
        var ctx     = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Title_Null_FailsValidation()
    {
        var model = new TodoFormViewModel { Title = null! };
        Validate(model).Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Title_Empty_FailsValidation()
    {
        var model = new TodoFormViewModel { Title = "" };
        Validate(model).Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Title_201Chars_FailsValidation()
    {
        var model = new TodoFormViewModel { Title = new string('x', 201) };
        Validate(model).Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Title_200Chars_PassesValidation()
    {
        var model = new TodoFormViewModel { Title = new string('x', 200) };
        Validate(model).Should().BeEmpty();
    }

    [Fact]
    public void Title_Valid_PassesValidation()
    {
        var model = new TodoFormViewModel { Title = "Buy milk" };
        Validate(model).Should().BeEmpty();
    }

    [Fact]
    public void Description_Null_PassesValidation()
    {
        var model = new TodoFormViewModel { Title = "Test", Description = null };
        Validate(model).Should().BeEmpty();
    }

    [Fact]
    public void DueDate_Null_PassesValidation()
    {
        var model = new TodoFormViewModel { Title = "Test", DueDate = null };
        Validate(model).Should().BeEmpty();
    }
}
