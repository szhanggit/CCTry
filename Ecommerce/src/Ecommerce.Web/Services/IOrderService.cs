using Ecommerce.Web.Models;

namespace Ecommerce.Web.Services;

public interface IOrderService
{
    Order PlaceOrder(CheckoutViewModel vm, Cart cart);
    Order? GetOrder(string orderNumber);
}
