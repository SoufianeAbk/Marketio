using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Marketio_Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        // GDPR Compliance Fields
        public DateTime ConsentGivenDate { get; set; } = DateTime.UtcNow;

        [Required]
        public bool TermsConsentGiven { get; set; }

        [Required]
        public bool PrivacyConsentGiven { get; set; }

        public bool MarketingOptIn { get; set; }

        // Account Deletion
        public bool IsDeletionRequested { get; set; }

        public DateTime? DeletionRequestedDate { get; set; }

        // Computed property voor volledige naam
        public string FullName => $"{FirstName} {LastName}";
    }
}