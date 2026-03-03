namespace Marketio_Web.Middleware
{
    public class CookieManagementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CookieManagementMiddleware> _logger;

        public CookieManagementMiddleware(RequestDelegate next, ILogger<CookieManagementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check for cookie consent
            var hasConsent = context.Request.Cookies.ContainsKey("cookie-consent");

            if (!hasConsent)
            {
                _logger.LogInformation(
                    "No cookie consent found for IP: {IP} | Path: {Path}",
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Path
                );
            }

            // Track cookie usage
            TrackCookies(context);

            // Set secure cookie options for new cookies
            context.Response.OnStarting(() =>
            {
                SetSecureCookieDefaults(context);
                return Task.CompletedTask;
            });

            await _next(context);

            // Log cookies after response
            LogResponseCookies(context);
        }

        private void TrackCookies(HttpContext context)
        {
            var essentialCookies = new List<string>();
            var functionalCookies = new List<string>();
            var analyticsCookies = new List<string>();

            foreach (var cookie in context.Request.Cookies)
            {
                // Categorize cookies
                if (IsEssentialCookie(cookie.Key))
                {
                    essentialCookies.Add(cookie.Key);
                }
                else if (IsFunctionalCookie(cookie.Key))
                {
                    functionalCookies.Add(cookie.Key);
                }
                else if (IsAnalyticsCookie(cookie.Key))
                {
                    analyticsCookies.Add(cookie.Key);
                }

                // Log cookie details
                _logger.LogDebug(
                    "Cookie Found: {CookieName} | Type: {CookieType} | Path: {Path}",
                    cookie.Key,
                    GetCookieType(cookie.Key),
                    context.Request.Path
                );
            }

            // Store cookie counts in context for later use
            context.Items["EssentialCookiesCount"] = essentialCookies.Count;
            context.Items["FunctionalCookiesCount"] = functionalCookies.Count;
            context.Items["AnalyticsCookiesCount"] = analyticsCookies.Count;
        }

        private void SetSecureCookieDefaults(HttpContext context)
        {
            // Apply secure defaults to all cookies
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            context.Items["DefaultCookieOptions"] = cookieOptions;
        }

        private void LogResponseCookies(HttpContext context)
        {
            if (context.Response.Headers.ContainsKey("Set-Cookie"))
            {
                var setCookieHeaders = context.Response.Headers["Set-Cookie"];
                _logger.LogInformation(
                    "Response Cookies Set: {Count} | Path: {Path} | Status: {StatusCode}",
                    setCookieHeaders.Count,
                    context.Request.Path,
                    context.Response.StatusCode
                );
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

        private bool IsFunctionalCookie(string cookieName)
        {
            var functionalCookies = new[]
            {
                ".AspNetCore.Culture",
                "cart-preferences",
                "language-preference"
            };

            return functionalCookies.Any(c => cookieName.StartsWith(c, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsAnalyticsCookie(string cookieName)
        {
            var analyticsCookies = new[]
            {
                "_ga",
                "_gid",
                "_gat",
                "analytics"
            };

            return analyticsCookies.Any(c => cookieName.StartsWith(c, StringComparison.OrdinalIgnoreCase));
        }

        private string GetCookieType(string cookieName)
        {
            if (IsEssentialCookie(cookieName)) return "Essential";
            if (IsFunctionalCookie(cookieName)) return "Functional";
            if (IsAnalyticsCookie(cookieName)) return "Analytics";
            return "Unknown";
        }
    }

    // Extension method for easy middleware registration
    public static class CookieManagementMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieManagement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieManagementMiddleware>();
        }
    }
}