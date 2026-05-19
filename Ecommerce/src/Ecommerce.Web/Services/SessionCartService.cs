using System.Text.Json;
using Ecommerce.Web.Models;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Web.Services;

public class SessionCartService : ICartService
{
    private const string SessionKey = "Cart";
    private readonly ISession _session;

    public SessionCartService(IHttpContextAccessor accessor)
    {
        _session = accessor.HttpContext!.Session;
    }

    public Cart GetCart()
    {
        var json = _session.GetString(SessionKey);
        return json is null
            ? new Cart()
            : JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
    }

    public void AddItem(Product product, int quantity)
    {
        var cart = GetCart();
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
            existing.Quantity += quantity;
        else
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = quantity
            });
        Save(cart);
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;

        if (quantity <= 0)
            cart.Items.Remove(item);
        else
            item.Quantity = quantity;

        Save(cart);
    }

    public void RemoveItem(int productId)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(i => i.ProductId == productId);
        Save(cart);
    }

    public void Clear()
    {
        _session.Remove(SessionKey);
    }

    private void Save(Cart cart) =>
        _session.SetString(SessionKey, JsonSerializer.Serialize(cart));
}
