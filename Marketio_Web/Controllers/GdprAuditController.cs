using Marketio_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketio_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GdprAuditController : Controller
    {
        private readonly IGdprAuditService _gdprAuditService;
        private readonly ILogger<GdprAuditController> _logger;

        public GdprAuditController(
            IGdprAuditService gdprAuditService,
            ILogger<GdprAuditController> logger)
        {
            _gdprAuditService = gdprAuditService;
            _logger = logger;
        }

        // GET: GdprAudit/AuditLogs
        public async Task<IActionResult> AuditLogs(DateTime? startDate, DateTime? endDate, string? eventType)
        {
            try
            {
                var logs = await _gdprAuditService.GetAllAuditLogsAsync(startDate, endDate, eventType);
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.EventType = eventType;
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                TempData["Error"] = "Kon audit logs niet ophalen.";
                return View(new List<GdprAuditLogDto>());
            }
        }

        // GET: GdprAudit/PendingDeletions
        public async Task<IActionResult> PendingDeletions()
        {
            try
            {
                var pendingRequests = await _gdprAuditService.GetPendingDeletionRequestsAsync();
                return View(pendingRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending deletions");
                TempData["Error"] = "Kon openstaande verzoeken niet ophalen.";
                return View(new List<GdprAuditLogDto>());
            }
        }

        // POST: GdprAudit/ProcessDeletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDeletion(string userId)
        {
            try
            {
                var adminEmail = User.Identity?.Name ?? "Unknown";
                await _gdprAuditService.MarkDeletionProcessedAsync(userId, adminEmail);

                _logger.LogWarning("Deletion processed for user {UserId} by {Admin}", userId, adminEmail);
                TempData["Success"] = "Verwijderingsverzoek verwerkt.";

                return RedirectToAction(nameof(PendingDeletions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deletion for user {UserId}", userId);
                TempData["Error"] = "Fout bij het verwerken van het verzoek.";
                return RedirectToAction(nameof(PendingDeletions));
            }
        }

        // GET: GdprAudit/UserAuditTrail/{userId}
        public async Task<IActionResult> UserAuditTrail(string userId)
        {
            try
            {
                var logs = await _gdprAuditService.GetUserAuditLogsAsync(userId);
                ViewBag.UserId = userId;
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for user {UserId}", userId);
                TempData["Error"] = "Kon audit trail niet ophalen.";
                return View(new List<GdprAuditLogDto>());
            }
        }
    }
}