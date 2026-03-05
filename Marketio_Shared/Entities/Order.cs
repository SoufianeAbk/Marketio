using System.ComponentModel.DataAnnotations;
using Marketio_Shared.Enums;

namespace Marketio_Shared.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Order number is required")]
        [MaxLength(50, ErrorMessage = "Order number cannot exceed 50 characters")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer ID is required")]
        [MaxLength(450, ErrorMessage = "Customer ID cannot exceed 450 characters")]
        public string CustomerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Order date is required")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Order status is required")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required(ErrorMessage = "Payment method is required")]
        public PaymentMethod PaymentMethod { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Total amount must be between 0.01 and 999999.99")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Shipping address is required")]
        [MaxLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Billing address is required")]
        [MaxLength(500, ErrorMessage = "Billing address cannot exceed 500 characters")]
        public string BillingAddress { get; set; } = string.Empty;

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}