using Ecommerce.Web.Models;

namespace Ecommerce.Web.Services;

public class InMemoryOrderService : IOrderService
{
    private readonly List<Order> _orders = new();

    public Order PlaceOrder(CheckoutViewModel vm, Cart cart)
    {
        var order = new Order
        {
            OrderNumber = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            PlacedAt = DateTime.UtcNow,
            CustomerName = vm.CustomerName,
            CustomerEmail = vm.CustomerEmail,
            ShippingAddress = vm.ShippingAddress,
            City = vm.City,
            PostCode = vm.PostCode,
            PaymentMethod = vm.PaymentMethod,
            Items = cart.Items.Select(i => new CartItem
            {
                ProductId = i.ProductId,
                Name = i.Name,
                Price = i.Price,
                ImageUrl = i.ImageUrl,
                Quantity = i.Quantity
            }).ToList(),
            Total = cart.TotalPrice
        };

        _orders.Add(order);
        return order;
    }

    public Order? GetOrder(string orderNumber) =>
        _orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
}
