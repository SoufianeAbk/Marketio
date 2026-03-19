using Marketio_Web.Models;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Marketio_Web.Controllers
{
    [Authorize]
    public class PrivacyController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGdprAuditService _gdprAuditService;
        private readonly ILogger<PrivacyController> _logger;

        public PrivacyController(
            UserManager<ApplicationUser> userManager,
            IGdprAuditService gdprAuditService,
            ILogger<PrivacyController> logger)
        {
            _userManager = userManager;
            _gdprAuditService = gdprAuditService;
            _logger = logger;
        }

        // GET: /Privacy/MyPrivacy
        public async Task<IActionResult> MyPrivacy()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var auditLogs = await _gdprAuditService.GetUserAuditLogsAsync(user.Id);
            ViewBag.AuditLogs = auditLogs;

            return View(user);
        }

        // POST: /Privacy/UpdateConsent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConsent(bool marketingOptIn)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                // Enkel marketing is intrekbaar — terms & privacy zijn verplicht bij registratie
                var changed = user.MarketingOptIn != marketingOptIn;
                user.MarketingOptIn = marketingOptIn;
                await _userManager.UpdateAsync(user);

                if (changed)
                {
                    await _gdprAuditService.LogConsentEventAsync(
                        user.Id,
                        "marketing",
                        marketingOptIn,
                        ip,
                        ua,
                        $"Marketing toestemming {(marketingOptIn ? "gegeven" : "ingetrokken")} via Mijn Privacy pagina");
                }

                TempData["Success"] = marketingOptIn
                    ? "Marketing toestemming ingeschakeld."
                    : "Marketing toestemming ingetrokken.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij bijwerken marketing consent voor gebruiker {UserId}", user.Id);
                TempData["Error"] = "Er is een fout opgetreden. Probeer het opnieuw.";
            }

            return RedirectToAction(nameof(MyPrivacy));
        }

        // POST: /Privacy/ExportData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                var personalData = await _gdprAuditService.ExportUserDataAsync(user.Id);
                if (personalData == null)
                {
                    TempData["Error"] = "Exporteren van gegevens mislukt.";
                    return RedirectToAction(nameof(MyPrivacy));
                }

                await _gdprAuditService.LogDataExportAsync(user.Id, ip, ua);

                var json = JsonSerializer.Serialize(personalData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var bytes = Encoding.UTF8.GetBytes(json);
                var fileName = $"marketio-mijn-gegevens-{DateTime.UtcNow:yyyy-MM-dd}.json";

                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij exporteren data voor gebruiker {UserId}", user.Id);
                TempData["Error"] = "Er is een fout opgetreden bij het exporteren. Probeer het opnieuw.";
                return RedirectToAction(nameof(MyPrivacy));
            }
        }

        // GET: /Privacy/RequestDeletion
        public async Task<IActionResult> RequestDeletion()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Toon bestaande RequestDeletion view met de user als model
            return View("~/Views/AccountDeletion/RequestDeletion.cshtml", user);
        }

        // POST: /Privacy/ConfirmDeletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeletion(string password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Verifieer wachtwoord
            var passwordCorrect = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordCorrect)
            {
                TempData["Error"] = "Ongeldig wachtwoord. Verwijderingsaanvraag geannuleerd.";
                return RedirectToAction(nameof(RequestDeletion));
            }

            // Controleer of er al een aanvraag loopt
            if (user.IsDeletionRequested)
            {
                TempData["Info"] = "Er is al een verwijderingsaanvraag voor uw account geregistreerd.";
                return RedirectToAction(nameof(MyPrivacy));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                await _gdprAuditService.LogDeletionRequestAsync(user.Id, ip, ua);

                _logger.LogWarning(
                    "Account verwijderingsaanvraag ingediend door gebruiker {UserId} | IP: {IP}",
                    user.Id, ip);

                // Uitloggen na de aanvraag
                TempData["Success"] = "Uw verwijderingsaanvraag is ontvangen. Uw account wordt binnen 30 dagen verwijderd conform de AVG.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwijderingsaanvraag voor gebruiker {UserId}", user.Id);
                TempData["Error"] = "Er is een fout opgetreden. Probeer het opnieuw of neem contact op.";
                return RedirectToAction(nameof(RequestDeletion));
            }
        }
    }
}