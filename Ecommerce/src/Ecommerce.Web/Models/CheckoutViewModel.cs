using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web.Models;

public class CheckoutViewModel
{
    [Required] [Display(Name = "Full Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required] [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required] [Display(Name = "Street Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required] [Display(Name = "Post Code")]
    public string PostCode { get; set; } = string.Empty;

    [Required] [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "CreditCard";

    public Cart Cart { get; set; } = new();
}
