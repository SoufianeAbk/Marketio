using Marketio_Shared.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Marketio_Shared.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Validation_AppUser_FirstName_Required")]
        [MaxLength(100, ErrorMessage = "Validation_AppUser_FirstName_MaxLength")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation_AppUser_LastName_Required")]
        [MaxLength(100, ErrorMessage = "Validation_AppUser_LastName_MaxLength")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Validation_AppUser_Address_MaxLength")]
        public string? Address { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // GDPR-velden

        public bool PrivacyConsentGiven { get; set; }
        public bool TermsConsentGiven { get; set; }
        public bool MarketingOptIn { get; set; }
        public DateTime? ConsentGivenDate { get; set; }

        // Account verwijderen (Recht om vergeten te worden)

        public bool IsDeletionRequested { get; set; }
        public DateTime? DeletionRequestedDate { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}