using Marketio_Web.Models;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Marketio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/account")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGdprAuditService _gdprAuditService;
        private readonly ILogger<AccountApiController> _logger;

        public AccountApiController(
            UserManager<ApplicationUser> userManager,
            IGdprAuditService gdprAuditService,
            ILogger<AccountApiController> logger)
        {
            _userManager = userManager;
            _gdprAuditService = gdprAuditService;
            _logger = logger;
        }

        // ─── Profile Management ────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/account/profile - Get current user's profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                address = user.Address,
                phoneNumber = user.PhoneNumber,
                privacyConsentGiven = user.PrivacyConsentGiven,
                termsConsentGiven = user.TermsConsentGiven,
                marketingOptIn = user.MarketingOptIn,
                consentGivenDate = user.ConsentGivenDate,
                isDeletionRequested = user.IsDeletionRequested,
                deletionRequestedDate = user.DeletionRequestedDate
            });
        }

        // ─── Consent Management ────────────────────────────────────────────────────

        /// <summary>
        /// POST /api/account/consent - Update marketing consent
        /// </summary>
        [HttpPost("consent")]
        public async Task<IActionResult> UpdateConsent([FromBody] UpdateConsentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                var changed = user.MarketingOptIn != request.MarketingOptIn;
                user.MarketingOptIn = request.MarketingOptIn;
                await _userManager.UpdateAsync(user);

                if (changed)
                {
                    await _gdprAuditService.LogConsentEventAsync(
                        userId,
                        "marketing",
                        request.MarketingOptIn,
                        ip,
                        ua,
                        $"Marketing toestemming {(request.MarketingOptIn ? "gegeven" : "ingetrokken")} via MAUI app");
                }

                _logger.LogInformation(
                    "User {UserId} updated marketing consent to {Value} via API",
                    userId,
                    request.MarketingOptIn);

                return Ok(new { message = "Consent updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consent for user {UserId}", userId);
                return BadRequest(new { message = "Failed to update consent" });
            }
        }

        // ─── Data Export (Right to Portability) ────────────────────────────────────

        /// <summary>
        /// GET /api/account/export-data - Export user's personal data as JSON
        /// </summary>
        [HttpGet("export-data")]
        public async Task<IActionResult> ExportData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var personalData = await _gdprAuditService.ExportUserDataAsync(userId);

                if (personalData == null)
                {
                    return NotFound(new { message = "No personal data found" });
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var ua = HttpContext.Request.Headers["User-Agent"].ToString();
                await _gdprAuditService.LogDataExportAsync(userId, ip, ua);

                var json = JsonSerializer.Serialize(personalData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var fileName = $"marketio-mijn-gegevens-{DateTime.UtcNow:yyyy-MM-dd}.json";

                _logger.LogInformation(
                    "User {UserId} exported personal data via API",
                    userId);

                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for user {UserId}", userId);
                return BadRequest(new { message = "Failed to export data" });
            }
        }

        // ─── Account Deletion (Right to be Forgotten) ──────────────────────────────

        /// <summary>
        /// POST /api/account/request-deletion - Request account deletion
        /// </summary>
        [HttpPost("request-deletion")]
        public async Task<IActionResult> RequestDeletion([FromBody] DeletionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Password is required" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Failed deletion request for user {UserId}: invalid password", userId);
                return BadRequest(new { message = "Invalid password" });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                // Immediately delete the account instead of just logging a request
                var result = await _gdprAuditService.DeleteUserAccountAsync(userId, "User");

                if (result)
                {
                    _logger.LogWarning(
                        "User {UserId} account permanently deleted via API | Email: {Email} | IP: {IP}",
                        userId,
                        user.Email,
                        ip);

                    return Ok(new { message = "Account successfully deleted" });
                }
                else
                {
                    _logger.LogError("Failed to delete account for user {UserId}", userId);
                    return BadRequest(new { message = "Failed to delete account" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing account deletion for user {UserId}", userId);
                return BadRequest(new { message = "Failed to process deletion request" });
            }
        }

        // ─── Audit Trail ──────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/account/audit-trail - Get user's GDPR audit trail
        /// </summary>
        [HttpGet("audit-trail")]
        public async Task<IActionResult> GetAuditTrail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var logs = await _gdprAuditService.GetUserAuditLogsAsync(userId);

                return Ok(logs.Select(log => new
                {
                    id = log.Id,
                    eventType = log.EventType,
                    timestamp = log.Timestamp,
                    consentType = log.ConsentType,
                    granted = log.Granted,
                    ipAddress = log.IpAddress,
                    details = log.Details,
                    processedDate = log.ProcessedDate,
                    processedBy = log.ProcessedBy
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for user {UserId}", userId);
                return BadRequest(new { message = "Failed to retrieve audit trail" });
            }
        }
    }

    public class UpdateConsentRequest
    {
        public bool MarketingOptIn { get; set; }
    }

    public class DeletionRequest
    {
        public string Password { get; set; } = string.Empty;
    }
}