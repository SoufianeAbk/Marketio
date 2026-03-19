namespace Marketio_Web.Services
{
    public class GdprAuditLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string ConsentType { get; set; } = string.Empty;
        public bool Granted { get; set; }
        public string? Details { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? ProcessedBy { get; set; }
    }

    public class UserPersonalDataDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime ExportDate { get; set; }
        public bool PrivacyConsentGiven { get; set; }
        public bool TermsConsentGiven { get; set; }
        public bool MarketingOptIn { get; set; }
        public DateTime ConsentGivenDate { get; set; }
        public List<object> Orders { get; set; } = new();
        public List<object> AuditLogs { get; set; } = new();
    }
}