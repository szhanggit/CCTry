using Ecommerce.Web.Models;

namespace Ecommerce.Web.Services;

public interface ICartService
{
    Cart GetCart();
    void AddItem(Product product, int quantity);
    void UpdateQuantity(int productId, int quantity);
    void RemoveItem(int productId);
    void Clear();
}
