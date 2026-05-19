using Ecommerce.Web.Models;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Ecommerce.Tests.Services;

public class SessionCartServiceTests
{
    private static SessionCartService BuildService(out MockSession session)
    {
        session = new MockSession();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(session);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
        return new SessionCartService(accessor.Object);
    }

    private static Product SampleProduct(int id = 1) => new()
    {
        Id = id, Name = "Test Product", Price = 10.00m, Stock = 5, ImageUrl = "/img/test.jpg"
    };

    [Fact]
    public void GetCart_Empty_ReturnsEmptyCart()
    {
        var sut = BuildService(out _);
        var cart = sut.GetCart();
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_NewProduct_AddsToCart()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(), 2);
        var cart = sut.GetCart();
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncrementsQuantity()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(), 1);
        sut.AddItem(SampleProduct(), 3);
        var cart = sut.GetCart();
        Assert.Single(cart.Items);
        Assert.Equal(4, cart.Items[0].Quantity);
    }

    [Fact]
    public void UpdateQuantity_SetsNewQuantity()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(), 2);
        sut.UpdateQuantity(1, 5);
        Assert.Equal(5, sut.GetCart().Items[0].Quantity);
    }

    [Fact]
    public void UpdateQuantity_ZeroOrLess_RemovesItem()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(), 2);
        sut.UpdateQuantity(1, 0);
        Assert.Empty(sut.GetCart().Items);
    }

    [Fact]
    public void RemoveItem_RemovesFromCart()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(1), 1);
        sut.AddItem(SampleProduct(2), 1);
        sut.RemoveItem(1);
        var cart = sut.GetCart();
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].ProductId);
    }

    [Fact]
    public void Clear_EmptiesCart()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(), 3);
        sut.Clear();
        Assert.Empty(sut.GetCart().Items);
    }

    [Fact]
    public void TotalPrice_CalculatesCorrectly()
    {
        var sut = BuildService(out _);
        sut.AddItem(SampleProduct(1), 2);  // 2 × $10 = $20
        sut.AddItem(new Product { Id = 2, Name = "B", Price = 5.00m, Stock = 10 }, 3); // 3 × $5 = $15
        Assert.Equal(35.00m, sut.GetCart().TotalPrice);
    }
}
