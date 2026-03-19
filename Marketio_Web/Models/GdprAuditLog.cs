using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marketio_Web.Models
{
    /// <summary>
    /// Centralized GDPR audit trail for tracking all consent, deletion, and data export activities
    /// </summary>
    public class GdprAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty; // e.g., "ConsentGiven", "ConsentWithdrawn", "DeletionRequested", "DataExported"

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        [MaxLength(50)]
        public string ConsentType { get; set; } = string.Empty; // "Privacy", "Marketing", "Terms", "Cookies", "All"

        public bool Granted { get; set; } // true = given/accepted, false = withdrawn/declined

        [MaxLength(1000)]
        public string? Details { get; set; } // JSON or serialized additional data

        public DateTime? ProcessedDate { get; set; } // For deletion requests - when it was actually processed

        [MaxLength(500)]
        public string? ProcessedBy { get; set; } // Admin email/id who processed the request

        // Foreign key
        [ForeignKey("ApplicationUser")]
        public string ApplicationUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}