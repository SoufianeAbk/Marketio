namespace Marketio_Web.Services
{
    /// <summary>
    /// Interface for GDPR compliance audit trail operations
    /// </summary>
    public interface IGdprAuditService
    {
        /// <summary>
        /// Log consent given/withdrawn event
        /// </summary>
        Task LogConsentEventAsync(
            string userId,
            string consentType,
            bool granted,
            string? ipAddress = null,
            string? userAgent = null,
            string? details = null);

        /// <summary>
        /// Log data deletion request
        /// </summary>
        Task LogDeletionRequestAsync(
            string userId,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Log data export request
        /// </summary>
        Task LogDataExportAsync(
            string userId,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Mark deletion request as processed
        /// </summary>
        Task MarkDeletionProcessedAsync(string userId, string? processedBy = null);

        /// <summary>
        /// Permanently delete user account and all related data (Right to be Forgotten)
        /// </summary>
        Task<bool> DeleteUserAccountAsync(string userId, string? processedBy = null);

        /// <summary>
        /// Get all audit logs for a user
        /// </summary>
        Task<List<GdprAuditLogDto>> GetUserAuditLogsAsync(string userId);

        /// <summary>
        /// Get all audit logs with optional filtering
        /// </summary>
        Task<List<GdprAuditLogDto>> GetAllAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? eventType = null);

        /// <summary>
        /// Get pending deletion requests
        /// </summary>
        Task<List<GdprAuditLogDto>> GetPendingDeletionRequestsAsync();

        /// <summary>
        /// Export user's personal data
        /// </summary>
        Task<UserPersonalDataDto?> ExportUserDataAsync(string userId);

        /// <summary>
        /// Check if user has active consent
        /// </summary>
        Task<bool> HasActiveConsentAsync(string userId, string consentType);
    }
}