using Ecommerce.Web.Models;

namespace Ecommerce.Web.Services;

public interface IProductService
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(int id);
    IReadOnlyList<Product> Search(string? keyword, string? category);
    IReadOnlyList<string> GetCategories();
}
