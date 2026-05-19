namespace Ecommerce.Web.Models;

public class Order
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}
