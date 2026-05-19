namespace Ecommerce.Web.Models;

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
    public int TotalItems => Items.Sum(i => i.Quantity);
    public decimal TotalPrice => Items.Sum(i => i.Subtotal);
}
