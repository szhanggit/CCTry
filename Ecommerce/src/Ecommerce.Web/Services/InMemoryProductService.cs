using System.Text.Json;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Services;

public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products;

    public InMemoryProductService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "products.json");
        var json = File.ReadAllText(path);
        _products = JsonSerializer.Deserialize<List<Product>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public IReadOnlyList<Product> GetAll() => _products;

    public Product? GetById(int id) =>
        _products.FirstOrDefault(p => p.Id == id);

    public IReadOnlyList<Product> Search(string? keyword, string? category)
    {
        var query = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p =>
                p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    public IReadOnlyList<string> GetCategories() =>
        _products.Select(p => p.Category).Distinct().Order().ToList();
}
