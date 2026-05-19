using Ecommerce.Web.Models;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class OrderController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public OrderController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    public IActionResult Checkout()
    {
        var cart = _cartService.GetCart();
        if (cart.TotalItems == 0) return RedirectToAction("Index", "Product");

        var vm = new CheckoutViewModel { Cart = cart };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Checkout(CheckoutViewModel vm)
    {
        var cart = _cartService.GetCart();
        vm.Cart = cart;

        if (!ModelState.IsValid) return View(vm);
        if (cart.TotalItems == 0)
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
            return View(vm);
        }

        var order = _orderService.PlaceOrder(vm, cart);
        _cartService.Clear();
        return RedirectToAction(nameof(Confirmation), new { orderNumber = order.OrderNumber });
    }

    public IActionResult Confirmation(string orderNumber)
    {
        var order = _orderService.GetOrder(orderNumber);
        if (order is null) return NotFound();
        return View(order);
    }
}
