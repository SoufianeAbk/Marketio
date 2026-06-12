using System.ComponentModel.DataAnnotations;
using Marketio_Shared.Enums;

namespace Marketio_Shared.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation_Order_OrderNumber_Required")]
        [MaxLength(50, ErrorMessage = "Validation_Order_OrderNumber_MaxLength")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Order_CustomerId_Required")]
        [MaxLength(450, ErrorMessage = "Validation_Order_CustomerId_MaxLength")]
        public string CustomerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Order_OrderDate_Required")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Validation_Order_Status_Required")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required(ErrorMessage = "Validation_Order_PaymentMethod_Required")]
        public PaymentMethod PaymentMethod { get; set; }

        [Required(ErrorMessage = "Validation_Order_TotalAmount_Required")]
        [Range(0.01, 999999.99, ErrorMessage = "Validation_Order_TotalAmount_Range")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Validation_Order_ShippingAddress_Required")]
        [MaxLength(500, ErrorMessage = "Validation_Order_ShippingAddress_MaxLength")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Order_BillingAddress_Required")]
        [MaxLength(500, ErrorMessage = "Validation_Order_BillingAddress_MaxLength")]
        public string BillingAddress { get; set; } = string.Empty;

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigatie properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
