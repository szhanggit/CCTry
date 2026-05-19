using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public IActionResult Index(string? keyword, string? category)
    {
        var products = _productService.Search(keyword, category);
        ViewBag.Categories = _productService.GetCategories();
        ViewBag.Keyword = keyword;
        ViewBag.Category = category;
        return View(products);
    }

    public IActionResult Detail(int id)
    {
        var product = _productService.GetById(id);
        if (product is null) return NotFound();
        return View(product);
    }
}
