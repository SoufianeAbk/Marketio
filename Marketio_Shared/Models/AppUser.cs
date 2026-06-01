using Marketio_Shared.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Marketio_Shared.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // ─── GDPR-velden ─────────────────────────────────────────────────────────

        public bool PrivacyConsentGiven { get; set; }
        public bool TermsConsentGiven { get; set; }
        public bool MarketingOptIn { get; set; }
        public DateTime? ConsentGivenDate { get; set; }

        // ─── Account deletion (Right to be Forgotten) ─────────────────────────

        public bool IsDeletionRequested { get; set; }
        public DateTime? DeletionRequestedDate { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
