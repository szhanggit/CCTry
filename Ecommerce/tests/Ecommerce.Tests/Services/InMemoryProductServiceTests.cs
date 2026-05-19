using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace Ecommerce.Tests.Services;

public class InMemoryProductServiceTests
{
    private readonly InMemoryProductService _sut;

    public InMemoryProductServiceTests()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath)
           .Returns(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "Ecommerce.Web"));
        _sut = new InMemoryProductService(env.Object);
    }

    [Fact]
    public void GetAll_ReturnsAllProducts()
    {
        var result = _sut.GetAll();
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetById_ExistingId_ReturnsProduct()
    {
        var result = _sut.GetById(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var result = _sut.GetById(99999);
        Assert.Null(result);
    }

    [Fact]
    public void Search_ByCategory_ReturnsOnlyMatchingCategory()
    {
        var result = _sut.Search(null, "Electronics");
        Assert.All(result, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public void Search_ByKeyword_ReturnsMatchingProducts()
    {
        var result = _sut.Search("keyboard", null);
        Assert.All(result, p =>
            Assert.True(p.Name.Contains("keyboard", StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains("keyboard", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Search_NoFilters_ReturnsAllProducts()
    {
        var all = _sut.GetAll();
        var result = _sut.Search(null, null);
        Assert.Equal(all.Count, result.Count);
    }

    [Fact]
    public void GetCategories_ReturnsDistinctSortedList()
    {
        var categories = _sut.GetCategories();
        Assert.Equal(categories.Count, categories.Distinct().Count());
        Assert.Equal(categories, categories.Order().ToList());
    }
}
