using System.Text;
using System.Text.Json;
using Marketio_App.Services;
using Microsoft.Extensions.Logging;

namespace Marketio_App.Services
{
    /// <summary>
    /// GDPR and account management API service for MAUI
    /// </summary>
    public class AccountApiService
    {
        private readonly ApiService _api;
        private readonly ILogger<AccountApiService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AccountApiService(ApiService api, ILogger<AccountApiService> logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ─── DTOs ─────────────────────────────────────────────────────────────────

        public class UserProfileDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Address { get; set; }
            public string? PhoneNumber { get; set; }
            public bool PrivacyConsentGiven { get; set; }
            public bool TermsConsentGiven { get; set; }
            public bool MarketingOptIn { get; set; }
            public DateTime? ConsentGivenDate { get; set; }
            public bool IsDeletionRequested { get; set; }
            public DateTime? DeletionRequestedDate { get; set; }
        }

        public class UpdateConsentRequest
        {
            public bool MarketingOptIn { get; set; }
        }

        public class DeletionRequestDto
        {
            public string Password { get; set; } = string.Empty;
        }

        public class AuditLogDto
        {
            public int Id { get; set; }
            public string EventType { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string ConsentType { get; set; } = string.Empty;
            public bool Granted { get; set; }
            public string? IpAddress { get; set; }
            public string? Details { get; set; }
            public DateTime? ProcessedDate { get; set; }
            public string? ProcessedBy { get; set; }
        }

        // ─── Profile Management ────────────────────────────────────────────────────

        /// <summary>
        /// Get current user's profile information
        /// </summary>
        public async Task<(bool Success, UserProfileDto? Profile, string? ErrorMessage)> GetProfileAsync()
        {
            _logger.LogDebug("[AccountApiService] GetProfileAsync called");

            try
            {
                var profile = await _api.GetAsync<UserProfileDto>("api/account/profile");

                if (profile == null)
                {
                    return (false, null, "Kon profielgegevens niet ophalen.");
                }

                _logger.LogInformation("[AccountApiService] Profile retrieved for user {Email}", profile.Email);
                return (true, profile, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountApiService] Error retrieving profile");
                return (false, null, $"Fout bij ophalen profiel: {ex.Message}");
            }
        }

        // ─── Consent Management ────────────────────────────────────────────────────

        /// <summary>
        /// Update marketing consent preference
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> UpdateConsentAsync(bool marketingOptIn)
        {
            _logger.LogDebug("[AccountApiService] UpdateConsentAsync called with MarketingOptIn={Value}", marketingOptIn);

            try
            {
                var request = new UpdateConsentRequest { MarketingOptIn = marketingOptIn };
                var response = await _api.PostAsync<UpdateConsentRequest, dynamic>(
                    "api/account/consent",
                    request,
                    allowNonSuccess: true
                );

                if (response == null)
                {
                    return (false, "Kon toestemming niet bijwerken.");
                }

                _logger.LogInformation("[AccountApiService] Consent updated: MarketingOptIn={Value}", marketingOptIn);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountApiService] Error updating consent");
                return (false, $"Fout bij bijwerken toestemming: {ex.Message}");
            }
        }

        // ─── Data Export (Right to Portability) ────────────────────────────────────

        /// <summary>
        /// Export personal data as JSON file.
        /// Hergebruikt de BaseAddress van de geconfigureerde ApiService zodat de URL
        /// op één centrale plek beheerd wordt (MauiProgram.GetPlatformApiBaseUrl).
        /// </summary>
        public async Task<(bool Success, byte[]? FileContent, string? FileName, string? ErrorMessage)> ExportDataAsync()
        {
            _logger.LogDebug("[AccountApiService] ExportDataAsync called");

            try
            {
                // Gebruik de BaseAddress van de geconfigureerde ApiService — geen losse URL meer
                var baseUrl = _api.BaseAddress?.ToString()
                    ?? throw new InvalidOperationException("ApiService heeft geen geconfigureerde BaseAddress.");

                // Bouw een tijdelijke HttpClient met dezelfde SSL-instellingen als in productie
#if DEBUG
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new HttpClient(handler);
#else
                using var client = new HttpClient();
#endif

                // Haal JWT op en zet Authorization-header
                var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync($"{baseUrl}api/account/export-data");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[AccountApiService] Export failed with status {Status}: {Error}",
                        response.StatusCode, errorBody);
                    return (false, null, null, "Fout bij exporteren: onverwachte respons van server.");
                }

                var fileContent = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"marketio-mijn-gegevens-{DateTime.UtcNow:yyyy-MM-dd}.json";

                _logger.LogInformation("[AccountApiService] Data exported successfully ({Size} bytes)", fileContent.Length);
                return (true, fileContent, fileName, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountApiService] Error exporting data");
                return (false, null, null, $"Fout bij exporteren: {ex.Message}");
            }
        }

        // ─── Account Deletion (Right to be Forgotten) ──────────────────────────────

        /// <summary>
        /// Request account deletion with password verification (immediately deletes the account)
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> RequestDeletionAsync(string password)
        {
            _logger.LogDebug("[AccountApiService] RequestDeletionAsync called");

            try
            {
                var request = new DeletionRequestDto { Password = password };
                var response = await _api.PostAsync<DeletionRequestDto, dynamic>(
                    "api/account/request-deletion",
                    request,
                    allowNonSuccess: true
                );

                if (response == null)
                {
                    return (false, "Kon verwijderingsaanvraag niet verwerken.");
                }

                _logger.LogWarning("[AccountApiService] Account deletion completed");
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountApiService] Error requesting deletion");
                return (false, $"Fout bij aanvraag: {ex.Message}");
            }
        }

        // ─── Audit Trail ──────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieve user's GDPR audit trail
        /// </summary>
        public async Task<(bool Success, List<AuditLogDto>? Logs, string? ErrorMessage)> GetAuditTrailAsync()
        {
            _logger.LogDebug("[AccountApiService] GetAuditTrailAsync called");

            try
            {
                var logs = await _api.GetAsync<List<AuditLogDto>>("api/account/audit-trail");

                if (logs == null)
                {
                    return (false, null, "Kon auditgegevens niet ophalen.");
                }

                _logger.LogInformation("[AccountApiService] Retrieved {Count} audit logs", logs.Count);
                return (true, logs, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountApiService] Error retrieving audit trail");
                return (false, null, $"Fout bij ophalen auditgegevens: {ex.Message}");
            }
        }
    }
}