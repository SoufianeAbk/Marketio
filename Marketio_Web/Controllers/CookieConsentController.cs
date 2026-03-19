using Microsoft.AspNetCore.Mvc;

namespace Marketio_Web.Controllers
{
    /// <summary>
    /// Handles AVG/GDPR cookie consent: accept or decline non-essential cookies.
    /// Sets a "CookieConsent" cookie that the _CookieBanner partial checks on every request.
    /// </summary>
    public class CookieConsentController : Controller
    {
        private const string ConsentCookieName = "CookieConsent";
        // Cookie geldig voor 1 jaar (standaard AVG-termijn voor consent)
        private static readonly TimeSpan ConsentCookieExpiry = TimeSpan.FromDays(365);

        private readonly ILogger<CookieConsentController> _logger;

        public CookieConsentController(ILogger<CookieConsentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gebruiker accepteert alle cookies. Stel consent-cookie in op "accepted".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(string? returnUrl = null)
        {
            SetConsentCookie("accepted");

            _logger.LogInformation(
                "Cookie consent ACCEPTED | IP: {IP} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                returnUrl ?? "/"
            );

            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        /// <summary>
        /// Gebruiker weigert niet-essentiële cookies. Stel consent-cookie in op "declined".
        /// Sessiecookies (essentieel) blijven actief.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(string? returnUrl = null)
        {
            SetConsentCookie("declined");

            _logger.LogInformation(
                "Cookie consent DECLINED | IP: {IP} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                returnUrl ?? "/"
            );

            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        /// <summary>
        /// Verwijdert de consent-cookie zodat de banner opnieuw verschijnt.
        /// Toegankelijk via een link in de Privacy Policy / footer.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Revoke(string? returnUrl = null)
        {
            Response.Cookies.Delete(ConsentCookieName);

            _logger.LogInformation(
                "Cookie consent REVOKED | IP: {IP} | Path: {Path}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                returnUrl ?? "/"
            );

            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private void SetConsentCookie(string value)
        {
            Response.Cookies.Append(ConsentCookieName, value, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.Add(ConsentCookieExpiry),
                HttpOnly = true,           // Niet toegankelijk via JavaScript
                Secure = true,             // Alleen via HTTPS
                SameSite = SameSiteMode.Lax,
                IsEssential = true         // Consent-cookie zelf is essentieel (AVG §6)
            });
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            // Voorkom open redirect aanvallen
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Content("~/") ?? "/";
        }
    }
}