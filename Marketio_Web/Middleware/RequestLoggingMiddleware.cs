namespace Marketio_Web.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var culture = context.Request.Cookies[".AspNetCore.Culture"];

            _logger.LogInformation(
                "Incoming Request: {Method} {Path} | IP: {IP} | Culture: {Culture} | User-Agent: {UserAgent}",
                requestMethod,
                requestPath,
                ipAddress,
                culture ?? "default",
                userAgent
            );

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request Failed: {Method} {Path} | Exception: {Message}",
                    requestMethod,
                    requestPath,
                    ex.Message
                );
                throw;
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                var statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "Request Completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms",
                    requestMethod,
                    requestPath,
                    statusCode,
                    duration.TotalMilliseconds
                );
            }
        }
    }
}