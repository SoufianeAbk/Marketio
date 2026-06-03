using System.ComponentModel.DataAnnotations;

namespace Marketio_Shared.Entities
{
    public class Customer
    {
        [Required(ErrorMessage = "Validation_Customer_Id_Required")]
        [MaxLength(450, ErrorMessage = "Validation_Customer_Id_MaxLength")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Customer_Email_Required")]
        [MaxLength(256, ErrorMessage = "Validation_Customer_Email_MaxLength")]
        [EmailAddress(ErrorMessage = "Validation_Customer_Email_Format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Customer_FirstName_Required")]
        [MaxLength(100, ErrorMessage = "Validation_Customer_FirstName_MaxLength")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Customer_LastName_Required")]
        [MaxLength(100, ErrorMessage = "Validation_Customer_LastName_MaxLength")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Customer_Phone_Required")]
        [MaxLength(20, ErrorMessage = "Validation_Customer_Phone_MaxLength")]
        [Phone(ErrorMessage = "Validation_Customer_Phone_Format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_Customer_Address_Required")]
        [MaxLength(500, ErrorMessage = "Validation_Customer_Address_MaxLength")]
        public string Address { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
