using System.ComponentModel.DataAnnotations;

namespace Marketio_Shared.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Order ID is required")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Unit price must be between 0.01 and 999999.99")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Total price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Total price must be between 0.01 and 999999.99")]
        public decimal TotalPrice { get; set; }

        // Navigatie properties
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}