using System.ComponentModel.DataAnnotations;

namespace Marketio_Shared.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation_OrderItem_OrderId_Required")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Validation_OrderItem_ProductId_Required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Validation_OrderItem_Quantity_Required")]
        [Range(1, 10000, ErrorMessage = "Validation_OrderItem_Quantity_Range")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Validation_OrderItem_UnitPrice_Required")]
        [Range(0.01, 999999.99, ErrorMessage = "Validation_OrderItem_UnitPrice_Range")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Validation_OrderItem_TotalPrice_Required")]
        [Range(0.01, 999999.99, ErrorMessage = "Validation_OrderItem_TotalPrice_Range")]
        public decimal TotalPrice { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigatie properties
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}