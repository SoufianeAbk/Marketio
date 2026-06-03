using System.ComponentModel.DataAnnotations;
using Marketio_Shared.Enums;

namespace Marketio_Shared.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation_Product_Name_Required")]
        [MaxLength(200, ErrorMessage = "Validation_Product_Name_MaxLength")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Product_Description_Required")]
        [MaxLength(2000, ErrorMessage = "Validation_Product_Description_MaxLength")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Product_Price_Required")]
        [Range(0.01, 999999.99, ErrorMessage = "Validation_Product_Price_Range")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Validation_Product_Stock_Required")]
        [Range(0, int.MaxValue, ErrorMessage = "Validation_Product_Stock_Range")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Validation_Product_Category_Required")]
        public ProductCategory Category { get; set; }

        [Required(ErrorMessage = "Validation_Product_ImageUrl_Required")]
        [MaxLength(500, ErrorMessage = "Validation_Product_ImageUrl_MaxLength")]
        [Url(ErrorMessage = "Validation_Product_ImageUrl_Url")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
