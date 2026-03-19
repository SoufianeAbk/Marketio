using Marketio_Web.Models;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketio_Web.Controllers
{
    [Authorize]
    public class AccountDeletionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGdprAuditService _gdprAuditService;
        private readonly ILogger<AccountDeletionController> _logger;

        public AccountDeletionController(
            UserManager<ApplicationUser> userManager,
            IGdprAuditService gdprAuditService,
            ILogger<AccountDeletionController> logger)
        {
            _userManager = userManager;
            _gdprAuditService = gdprAuditService;
            _logger = logger;
        }

        // GET: AccountDeletion/RequestDeletion
        public IActionResult RequestDeletion()
        {
            return View();
        }

        // POST: AccountDeletion/RequestDeletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDeletion(string password)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                ModelState.AddModelError("password", "Het wachtwoord is onjuist.");
                return View();
            }

            try
            {
                // Log deletion request in GDPR audit trail
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers.UserAgent.ToString();

                await _gdprAuditService.LogDeletionRequestAsync(userId, ipAddress, userAgent);

                _logger.LogWarning(
                    "User {UserId} requested account deletion | Email: {Email}",
                    userId,
                    user.Email
                );

                TempData["Success"] = "Uw verwijderingsverzoek is ingediend. Een beheerder zal dit binnenkort verwerken.";
                return RedirectToAction("DeletionConfirmed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deletion request for user {UserId}", userId);
                ModelState.AddModelError("", "Er is een fout opgetreden bij het verwerken van uw verzoek.");
                return View();
            }
        }

        // GET: AccountDeletion/DeletionConfirmed
        public IActionResult DeletionConfirmed()
        {
            return View();
        }

        // GET: AccountDeletion/ExportData
        public async Task<IActionResult> ExportData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var personalData = await _gdprAuditService.ExportUserDataAsync(userId);

                if (personalData == null)
                {
                    TempData["Error"] = "Kon geen gegevens voor u vinden.";
                    return RedirectToAction("Index", "Home");
                }

                // Log export request
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers.UserAgent.ToString();
                await _gdprAuditService.LogDataExportAsync(userId, ipAddress, userAgent);

                // Return as JSON download
                var json = System.Text.Json.JsonSerializer.Serialize(personalData,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                var fileName = $"personal-data-{DateTime.UtcNow:yyyy-MM-dd}.json";
                return File(System.Text.Encoding.UTF8.GetBytes(json),
                    "application/json",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for user {UserId}", userId);
                TempData["Error"] = "Er is een fout opgetreden bij het exporteren van uw gegevens.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: AccountDeletion/ViewAuditTrail
        public async Task<IActionResult> ViewAuditTrail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var auditLogs = await _gdprAuditService.GetUserAuditLogsAsync(userId);
                return View(auditLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
                TempData["Error"] = "Kon audit logs niet ophalen.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}