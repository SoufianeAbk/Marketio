using Marketio_Shared.Data;
using Marketio_Shared.Entities;
using Marketio_Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Marketio_Web.Services
{
    /// <summary>
    /// Central logging service voor alle GDPR compliance activiteiten.
    /// </summary>
    public class GdprAuditService : IGdprAuditService
    {
        private readonly MarketioDbContext _context;
        private readonly ILogger<GdprAuditService> _logger;
        private readonly UserManager<AppUser> _userManager;

        public GdprAuditService(
            MarketioDbContext context,
            ILogger<GdprAuditService> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

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

                // Update AppUser consent fields
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

                // Stap 1: verwijder orders en order items
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

                // Stap 2: verbreek FK-referenties in GdprAuditLogs vóór verwijdering
                var auditLogs = await _context.GdprAuditLogs
                    .Where(x => x.ApplicationUserId == userId)
                    .ToListAsync();

                foreach (var log in auditLogs)
                {
                    log.ApplicationUserId = null;
                }

                _context.GdprAuditLogs.UpdateRange(auditLogs);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleared ApplicationUserId from {LogCount} audit logs for user {UserId}",
                    auditLogs.Count, userId);

                // Stap 3: verwijder gebruiker
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    // Stap 4: voeg definitieve verwijderingslog toe
                    var deletionLog = new GdprAuditLog
                    {
                        UserId = userId,
                        ApplicationUserId = null,
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

        public async Task<List<GdprAuditLogDto>> GetAllAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? eventType = null)
        {
            try
            {
                var query = _context.GdprAuditLogs.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(x => x.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(x => x.Timestamp <= endDate.Value);

                if (!string.IsNullOrWhiteSpace(eventType))
                    query = query.Where(x => x.EventType == eventType);

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
                    ConsentGivenDate = user.ConsentGivenDate ?? DateTime.MinValue,
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

        public async Task<bool> HasActiveConsentAsync(string userId, string consentType)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

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
