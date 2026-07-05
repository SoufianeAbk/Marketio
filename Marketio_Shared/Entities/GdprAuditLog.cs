using Marketio_Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marketio_Shared.Entities
{
    /// <summary>
    /// GDPR audit log entry — registreert elke consent-actie, data-export of verwijderingsaanvraag.
    /// Behoort tot Shared zodat alle platforms (Web) hetzelfde schema delen.
    /// </summary>
    public class GdprAuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Identity user ID — bewaard ook na verwijdering van het account.</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Nullable FK naar AppUser — wordt null gezet vóór account-verwijdering.</summary>
        [MaxLength(450)]
        public string? ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual AppUser? AppUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ConsentType { get; set; }

        public bool Granted { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public string? Details { get; set; }

        public DateTime? ProcessedDate { get; set; }

        [MaxLength(256)]
        public string? ProcessedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }
}