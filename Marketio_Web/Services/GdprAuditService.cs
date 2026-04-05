using Marketio_Web.Data;
using Marketio_Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Marketio_Web.Services
{
    /// <summary>
    /// Central logging service for all GDPR compliance activities
    /// </summary>
    public class GdprAuditService : IGdprAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GdprAuditService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public GdprAuditService(
            ApplicationDbContext context,
            ILogger<GdprAuditService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Logs consent given or withdrawn events with full audit trail
        /// </summary>
        public async Task LogConsentEventAsync(
            string userId,
            string consentType,
            bool granted,
            string? ipAddress = null,
            string? userAgent = null,
            string? details = null)
        {
            try
            {
                var auditLog = new GdprAuditLog
                {
                    UserId = userId,
                    ApplicationUserId = userId,
                    EventType = granted ? "ConsentGiven" : "ConsentWithdrawn",
                    ConsentType = consentType,
                    Granted = granted,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Details = details
                };

                _context.GdprAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "GDPR Audit: Consent {Event} for user {UserId} | Type: {ConsentType} | IP: {IP}",
                    granted ? "GIVEN" : "WITHDRAWN",
                    userId,
                    consentType,
                    ipAddress ?? "Unknown"
                );

                // Update ApplicationUser consent fields
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    switch (consentType.ToLower())
                    {
                        case "privacy":
                            user.PrivacyConsentGiven = granted;
                            break;
                        case "terms":
                            user.TermsConsentGiven = granted;
                            break;
                        case "marketing":
                            user.MarketingOptIn = granted;
                            break;
                    }

                    if (granted)
                    {
                        user.ConsentGivenDate = DateTime.UtcNow;
                    }

                    await _userManager.UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging consent event for user {UserId} | ConsentType: {ConsentType}",
                    userId, consentType);
                throw;
            }
        }

        /// <summary>
        /// Logs when user requests account deletion (Right to be Forgotten)
        /// </summary>
        public async Task LogDeletionRequestAsync(
            string userId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                var auditLog = new GdprAuditLog
                {
                    UserId = userId,
                    ApplicationUserId = userId,
                    EventType = "DeletionRequested",
                    ConsentType = "All",
                    Granted = false,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Details = JsonSerializer.Serialize(new { RequestedAt = DateTime.UtcNow })
                };

                _context.GdprAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Mark user as deletion requested
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.IsDeletionRequested = true;
                    user.DeletionRequestedDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogWarning(
                    "GDPR Audit: DELETION REQUESTED for user {UserId} | IP: {IP} | Timestamp: {Timestamp}",
                    userId,
                    ipAddress ?? "Unknown",
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging deletion request for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Logs when user exports their personal data
        /// </summary>
        public async Task LogDataExportAsync(
            string userId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                var auditLog = new GdprAuditLog
                {
                    UserId = userId,
                    ApplicationUserId = userId,
                    EventType = "DataExported",
                    ConsentType = "All",
                    Granted = true,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Details = JsonSerializer.Serialize(new { ExportedAt = DateTime.UtcNow })
                };

                _context.GdprAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "GDPR Audit: DATA EXPORTED for user {UserId} | IP: {IP} | Timestamp: {Timestamp}",
                    userId,
                    ipAddress ?? "Unknown",
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging data export for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Marks a deletion request as processed
        /// </summary>
        public async Task MarkDeletionProcessedAsync(string userId, string? processedBy = null)
        {
            try
            {
                var deletionLog = await _context.GdprAuditLogs
                    .Where(x => x.UserId == userId && x.EventType == "DeletionRequested" && x.ProcessedDate == null)
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();

                if (deletionLog != null)
                {
                    deletionLog.ProcessedDate = DateTime.UtcNow;
                    deletionLog.ProcessedBy = processedBy;
                    _context.GdprAuditLogs.Update(deletionLog);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "GDPR Audit: DELETION PROCESSED for user {UserId} | ProcessedBy: {ProcessedBy} | Timestamp: {Timestamp}",
                        userId,
                        processedBy ?? "System",
                        DateTime.UtcNow
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking deletion as processed for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Permanently deletes user account and all related data (Right to be Forgotten)
        /// </summary>
        public async Task<bool> DeleteUserAccountAsync(string userId, string? processedBy = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for deletion", userId);
                    return false;
                }

                // Step 1: Delete user orders and order items FIRST
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == userId)
                    .Include(o => o.OrderItems)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    _context.OrderItems.RemoveRange(order.OrderItems);
                    _context.Orders.Remove(order);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {OrderCount} orders for user {UserId}", orders.Count, userId);

                // Step 2: Clear ApplicationUserId foreign key references in GdprAuditLogs
                // This breaks the FK constraint before deletion
                var auditLogs = await _context.GdprAuditLogs
                    .Where(x => x.ApplicationUserId == userId)
                    .ToListAsync();

                foreach (var log in auditLogs)
                {
                    log.ApplicationUserId = null; // Clear the FK reference
                }

                _context.GdprAuditLogs.UpdateRange(auditLogs);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleared ApplicationUserId from {LogCount} audit logs for user {UserId}",
                    auditLogs.Count, userId);

                // Step 3: Delete user account
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    // Step 4: Add final deletion record after user is deleted
                    var deletionLog = new GdprAuditLog
                    {
                        UserId = userId,
                        ApplicationUserId = null, // User no longer exists
                        EventType = "DeletionCompleted",
                        ConsentType = "All",
                        Granted = false,
                        Timestamp = DateTime.UtcNow,
                        ProcessedDate = DateTime.UtcNow,
                        ProcessedBy = processedBy ?? "System",
                        Details = JsonSerializer.Serialize(new { DeletedAt = DateTime.UtcNow, Email = user.Email })
                    };

                    _context.GdprAuditLogs.Add(deletionLog);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "GDPR Audit: ACCOUNT PERMANENTLY DELETED for user {UserId} | Email: {Email} | ProcessedBy: {ProcessedBy} | Timestamp: {Timestamp}",
                        userId,
                        user.Email,
                        processedBy ?? "User",
                        DateTime.UtcNow
                    );

                    return true;
                }
                else
                {
                    _logger.LogError(
                        "GDPR Audit: ACCOUNT DELETION FAILED for user {UserId} | Errors: {Errors}",
                        userId,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting user account {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all audit logs for a specific user
        /// </summary>
        public async Task<List<GdprAuditLogDto>> GetUserAuditLogsAsync(string userId)
        {
            try
            {
                var logs = await _context.GdprAuditLogs
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Timestamp)
                    .Select(x => MapToDto(x))
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all audit logs with optional filtering
        /// </summary>
        public async Task<List<GdprAuditLogDto>> GetAllAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? eventType = null)
        {
            try
            {
                var query = _context.GdprAuditLogs.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp <= endDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(eventType))
                {
                    query = query.Where(x => x.EventType == eventType);
                }

                var logs = await query
                    .OrderByDescending(x => x.Timestamp)
                    .Select(x => MapToDto(x))
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all audit logs");
                throw;
            }
        }

        /// <summary>
        /// Retrieves pending deletion requests
        /// </summary>
        public async Task<List<GdprAuditLogDto>> GetPendingDeletionRequestsAsync()
        {
            try
            {
                var logs = await _context.GdprAuditLogs
                    .Where(x => x.EventType == "DeletionRequested" && x.ProcessedDate == null)
                    .OrderBy(x => x.Timestamp)
                    .Select(x => MapToDto(x))
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending deletion requests");
                throw;
            }
        }

        /// <summary>
        /// Exports user's complete personal data in machine-readable format
        /// </summary>
        public async Task<UserPersonalDataDto?> ExportUserDataAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for data export", userId);
                    return null;
                }

                var orders = await _context.Orders
                    .Where(o => o.CustomerId == userId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ToListAsync();

                var auditLogs = await _context.GdprAuditLogs
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();

                var personalData = new UserPersonalDataDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    ExportDate = DateTime.UtcNow,
                    PrivacyConsentGiven = user.PrivacyConsentGiven,
                    TermsConsentGiven = user.TermsConsentGiven,
                    MarketingOptIn = user.MarketingOptIn,
                    ConsentGivenDate = user.ConsentGivenDate,
                    Orders = orders.Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        Items = o.OrderItems.Select(oi => new
                        {
                            oi.ProductId,
                            oi.Product.Name,
                            oi.Quantity,
                            oi.UnitPrice
                        }).ToList()
                    }).Cast<object>().ToList(),
                    AuditLogs = auditLogs.Select(al => new
                    {
                        al.EventType,
                        al.Timestamp,
                        al.ConsentType,
                        al.Granted,
                        al.IpAddress
                    }).Cast<object>().ToList()
                };

                _logger.LogInformation(
                    "GDPR Audit: DATA EXPORT created for user {UserId} | Orders: {OrderCount} | Audit Logs: {LogCount}",
                    userId,
                    orders.Count,
                    auditLogs.Count
                );

                return personalData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Checks if user has active consent for a specific type
        /// </summary>
        public async Task<bool> HasActiveConsentAsync(string userId, string consentType)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                return consentType.ToLower() switch
                {
                    "privacy" => user.PrivacyConsentGiven,
                    "terms" => user.TermsConsentGiven,
                    "marketing" => user.MarketingOptIn,
                    "all" => user.PrivacyConsentGiven && user.TermsConsentGiven,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking consent for user {UserId}", userId);
                return false;
            }
        }

        private static GdprAuditLogDto MapToDto(GdprAuditLog log)
        {
            return new GdprAuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                EventType = log.EventType,
                Timestamp = log.Timestamp,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                ConsentType = log.ConsentType,
                Granted = log.Granted,
                Details = log.Details,
                ProcessedDate = log.ProcessedDate,
                ProcessedBy = log.ProcessedBy
            };
        }
    }
}