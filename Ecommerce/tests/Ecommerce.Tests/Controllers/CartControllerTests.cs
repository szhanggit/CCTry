using Ecommerce.Web.Controllers;
using Ecommerce.Web.Models;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Ecommerce.Tests.Controllers;

public class CartControllerTests
{
    private static (CartController controller, Mock<ICartService> cartMock) Build(
        Mock<IProductService>? productMock = null)
    {
        var cart = new Mock<ICartService>();
        var product = productMock ?? new Mock<IProductService>();
        return (new CartController(cart.Object, product.Object), cart);
    }

    [Fact]
    public void Add_UnknownProduct_ReturnsFailureJson()
    {
        var productMock = new Mock<IProductService>();
        productMock.Setup(s => s.GetById(99)).Returns((Product?)null);
        var (controller, _) = Build(productMock);

        var result = controller.Add(99, 1) as JsonResult;
        Assert.NotNull(result);
        dynamic data = result!.Value!;
        Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
    }

    [Fact]
    public void Add_KnownProduct_ReturnsSuccessJson()
    {
        var productMock = new Mock<IProductService>();
        productMock.Setup(s => s.GetById(1)).Returns(new Product { Id = 1, Name = "P", Price = 9.99m });

        var cartMock = new Mock<ICartService>();
        cartMock.Setup(c => c.GetCart()).Returns(new Cart { Items = new() { new CartItem { ProductId = 1, Quantity = 1 } } });

        var controller = new CartController(cartMock.Object, productMock.Object);
        var result = controller.Add(1, 1) as JsonResult;

        Assert.NotNull(result);
        dynamic data = result!.Value!;
        Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
    }

    [Fact]
    public void Count_ReturnsCartItemCount()
    {
        var cartMock = new Mock<ICartService>();
        cartMock.Setup(c => c.GetCart()).Returns(new Cart
        {
            Items = new() { new CartItem { ProductId = 1, Quantity = 3 } }
        });

        var controller = new CartController(cartMock.Object, new Mock<IProductService>().Object);
        var result = controller.Count() as JsonResult;

        Assert.NotNull(result);
        dynamic data = result!.Value!;
        Assert.Equal(3, (int)data.GetType().GetProperty("totalItems")!.GetValue(data)!);
    }
}
