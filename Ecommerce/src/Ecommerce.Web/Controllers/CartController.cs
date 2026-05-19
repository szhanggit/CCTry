using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IProductService _productService;

    public CartController(ICartService cartService, IProductService productService)
    {
        _cartService = cartService;
        _productService = productService;
    }

    public IActionResult Index()
    {
        var cart = _cartService.GetCart();
        return View(cart);
    }

    public IActionResult Count()
    {
        var cart = _cartService.GetCart();
        return Json(new { totalItems = cart.TotalItems });
    }

    [HttpPost]
    public IActionResult Add(int productId, int quantity = 1)
    {
        var product = _productService.GetById(productId);
        if (product is null)
            return Json(new { success = false, message = "Product not found." });

        _cartService.AddItem(product, quantity);
        var cart = _cartService.GetCart();
        return Json(new { success = true, totalItems = cart.TotalItems });
    }

    [HttpPost]
    public IActionResult Update(int productId, int quantity)
    {
        _cartService.UpdateQuantity(productId, quantity);
        var cart = _cartService.GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        return Json(new
        {
            success = true,
            subtotal = item?.Subtotal.ToString("C") ?? "$0.00",
            total = cart.TotalPrice.ToString("C"),
            totalItems = cart.TotalItems
        });
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        _cartService.RemoveItem(productId);
        var cart = _cartService.GetCart();
        return Json(new
        {
            success = true,
            total = cart.TotalPrice.ToString("C"),
            totalItems = cart.TotalItems
        });
    }
}
