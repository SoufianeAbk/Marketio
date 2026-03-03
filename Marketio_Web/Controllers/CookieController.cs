using Microsoft.AspNetCore.Mvc;

namespace Marketio_Web.Controllers
{
    public class CookieController : Controller
    {
        private readonly ILogger<CookieController> _logger;

        public CookieController(ILogger<CookieController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult AcceptCookies(string returnUrl = "/")
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            };

            Response.Cookies.Append("cookie-consent", "accepted", cookieOptions);

            _logger.LogInformation(
                "Cookie consent accepted | IP: {IP} | User-Agent: {UserAgent}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()
            );

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public IActionResult DeclineCookies(string returnUrl = "/")
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            };

            Response.Cookies.Append("cookie-consent", "declined", cookieOptions);

            // Remove non-essential cookies
            RemoveNonEssentialCookies();

            _logger.LogInformation(
                "Cookie consent declined | IP: {IP}",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public IActionResult ManageCookiePreferences(bool analytics, bool functional, string returnUrl = "/")
        {
            var preferences = $"analytics:{analytics};functional:{functional}";

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            };

            Response.Cookies.Append("cookie-consent", "custom", cookieOptions);
            Response.Cookies.Append("cookie-preferences", preferences, cookieOptions);

            if (!analytics)
            {
                RemoveAnalyticsCookies();
            }

            if (!functional)
            {
                RemoveFunctionalCookies();
            }

            _logger.LogInformation(
                "Cookie preferences updated | Analytics: {Analytics} | Functional: {Functional} | IP: {IP}",
                analytics,
                functional,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        public IActionResult GetCookieStatus()
        {
            var hasConsent = Request.Cookies.ContainsKey("cookie-consent");
            var consentValue = Request.Cookies["cookie-consent"];
            var preferences = Request.Cookies["cookie-preferences"];

            return Json(new
            {
                hasConsent,
                consentValue,
                preferences
            });
        }

        private void RemoveNonEssentialCookies()
        {
            var cookiesToRemove = Request.Cookies.Keys
                .Where(key => !IsEssentialCookie(key))
                .ToList();

            foreach (var cookieName in cookiesToRemove)
            {
                Response.Cookies.Delete(cookieName);
                _logger.LogDebug("Removed non-essential cookie: {CookieName}", cookieName);
            }
        }

        private void RemoveAnalyticsCookies()
        {
            var analyticsCookies = new[] { "_ga", "_gid", "_gat", "analytics" };
            foreach (var cookieName in analyticsCookies)
            {
                if (Request.Cookies.ContainsKey(cookieName))
                {
                    Response.Cookies.Delete(cookieName);
                    _logger.LogDebug("Removed analytics cookie: {CookieName}", cookieName);
                }
            }
        }

        private void RemoveFunctionalCookies()
        {
            var functionalCookies = new[] { "cart-preferences", "language-preference" };
            foreach (var cookieName in functionalCookies)
            {
                if (Request.Cookies.ContainsKey(cookieName))
                {
                    Response.Cookies.Delete(cookieName);
                    _logger.LogDebug("Removed functional cookie: {CookieName}", cookieName);
                }
            }
        }

        private bool IsEssentialCookie(string cookieName)
        {
            var essentialCookies = new[]
            {
                ".AspNetCore.Antiforgery",
                ".AspNetCore.Session",
                ".AspNetCore.Identity.Application",
                "cookie-consent"
            };

            return essentialCookies.Any(c => cookieName.StartsWith(c, StringComparison.OrdinalIgnoreCase));
        }
    }
}