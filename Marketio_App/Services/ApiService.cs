using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Marketio_App.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;
        private readonly ILogger<ApiService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private const string JwtKey = "jwt_token";
        private const string RefreshTokenKey = "refresh_token";  // nieuw

        // Voorkomt oneindige recursie als refresh zelf ook een 401 teruggeeft
        private bool _isRefreshing;

        /// <summary>
        /// Exposes the base API URL for constructing absolute URLs (e.g., for images)
        /// </summary>
        public Uri? BaseAddress => _client.BaseAddress;

        /// <summary>
        /// Geeft toegang tot de geconfigureerde HttpClient (inclusief handler en BaseAddress).
        /// Gebruik dit in plaats van een losse new HttpClient() aan te maken,
        /// zodat de SSL-bypass handler en platform-URL consistent blijven.
        /// </summary>
        public HttpClient HttpClient => _client;

        /// <summary>
        /// Raised when a 401 Unauthorized response is received AND the refresh token
        /// also failed (or was absent). The app should navigate to the login page.
        /// </summary>
        public event EventHandler? TokenExpired;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("[ApiService] Created. BaseAddress = {Base}", _client.BaseAddress);
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            _logger.LogDebug("[ApiService] InitializeAsync started.");
            try
            {
                var token = await SecureStorage.Default.GetAsync(JwtKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    SetAuthorizationHeader(token);
                    _logger.LogDebug("[ApiService] Stored JWT loaded into Authorization header.");
                }
                else
                {
                    _logger.LogDebug("[ApiService] No stored JWT found.");
                }
            }
            catch (Exception ex)
            {
                // SecureStorage can throw on emulators / certain Android configs
                _logger.LogWarning(ex, "[ApiService] SecureStorage.GetAsync failed during init — continuing without token.");
            }
        }

        private async Task EnsureAuthHeaderAsync()
        {
            if (_client.DefaultRequestHeaders.Authorization != null)
                return; // already set

            try
            {
                var token = await SecureStorage.Default.GetAsync(JwtKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    SetAuthorizationHeader(token);
                    _logger.LogDebug("[ApiService] EnsureAuthHeader: token restored from SecureStorage.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ApiService] EnsureAuthHeader: SecureStorage.GetAsync failed.");
            }
        }

        // ─── Token management ─────────────────────────────────────────────────────

        public void SetAuthorizationHeader(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _client.DefaultRequestHeaders.Authorization = null;
                _logger.LogDebug("[ApiService] Authorization header cleared.");
                return;
            }
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("[ApiService] Authorization header set (token length={Len}).", token.Length);
        }

        public async Task SaveTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("[ApiService] SaveTokenAsync called with empty token — ignored.");
                return;
            }
            try
            {
                await SecureStorage.Default.SetAsync(JwtKey, token);
                _logger.LogDebug("[ApiService] JWT persisted to SecureStorage.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ApiService] SecureStorage.SetAsync failed — token kept in memory only.");
            }
            finally
            {
                // Always apply to header, even if persistence fails
                SetAuthorizationHeader(token);
            }
        }

        /// <summary>Slaat het refresh token op in SecureStorage.</summary>
        public async Task SaveRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("[ApiService] SaveRefreshTokenAsync called with empty token — ignored.");
                return;
            }
            try
            {
                await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
                _logger.LogDebug("[ApiService] Refresh token persisted to SecureStorage.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ApiService] SecureStorage.SetAsync (refresh) failed.");
            }
        }

        public async Task ClearTokenAsync()
        {
            _logger.LogDebug("[ApiService] ClearTokenAsync called — JWT + refresh token wissen.");
            try { SecureStorage.Default.Remove(JwtKey); }
            catch (Exception ex) { _logger.LogWarning(ex, "[ApiService] SecureStorage.Remove (JWT) failed."); }

            try { SecureStorage.Default.Remove(RefreshTokenKey); }
            catch (Exception ex) { _logger.LogWarning(ex, "[ApiService] SecureStorage.Remove (refresh) failed."); }

            _client.DefaultRequestHeaders.Authorization = null;
            await Task.CompletedTask;
        }

        // ─── Silent token refresh (401-interceptor) ───────────────────────────────

        /// <summary>
        /// Probeert het toegangstoken te vernieuwen via het opgeslagen refresh token.
        /// Retourneert true als de vernieuwing slaagde; anders false.
        /// </summary>
        private async Task<bool> TryRefreshTokenInternalAsync()
        {
            if (_isRefreshing)
                return false; // circulaire aanroep voorkomen

            _isRefreshing = true;
            try
            {
                var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogDebug("[ApiService] TryRefresh: geen refresh token beschikbaar.");
                    return false;
                }

                _logger.LogDebug("[ApiService] TryRefresh: refresh token aanwezig, aanvraag versturen.");

                // Directe HTTP-aanroep (geen interceptors, geen recursie)
                var jsonPayload = JsonSerializer.Serialize(new { refreshToken });
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var res = await _client.PostAsync("api/auth/refresh", content);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[ApiService] TryRefresh: server retourneerde {Status}.", (int)res.StatusCode);
                    return false;
                }

                var json = await res.Content.ReadAsStringAsync();
                if (!IsValidJson(json))
                {
                    _logger.LogWarning("[ApiService] TryRefresh: ongeldige JSON in antwoord.");
                    return false;
                }

                var response = JsonSerializer.Deserialize<RefreshResponse>(json, _jsonOptions);
                if (response?.Token == null)
                {
                    _logger.LogWarning("[ApiService] TryRefresh: leeg token in antwoord.");
                    return false;
                }

                // Nieuw JWT opslaan + Authorization header updaten
                await SaveTokenAsync(response.Token);

                // Nieuw refresh token opslaan (token-rotatie)
                if (!string.IsNullOrWhiteSpace(response.RefreshToken))
                    await SaveRefreshTokenAsync(response.RefreshToken);

                _logger.LogInformation("[ApiService] TryRefresh: token succesvol vernieuwd.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ApiService] TryRefresh: onverwachte fout.");
                return false;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Voert het HTTP-verzoek uit. Bij een 401 wordt eerst geprobeerd het token
        /// te vernieuwen en het verzoek opnieuw te versturen. Pas als ook dat mislukt,
        /// wordt <see cref="TokenExpired"/> getriggerd.
        /// </summary>
        private async Task<HttpResponseMessage> SendWithRefreshAsync(
            Func<Task<HttpResponseMessage>> requestFactory)
        {
            var response = await requestFactory();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogDebug("[ApiService] SendWithRefresh: 401 ontvangen — refresh proberen.");

                if (await TryRefreshTokenInternalAsync())
                {
                    response.Dispose(); // gooi de 401-response weg
                    response = await requestFactory(); // herhaal met nieuw token
                    _logger.LogDebug("[ApiService] SendWithRefresh: herhaald verzoek → {Status}", (int)response.StatusCode);
                }
            }

            return response; // aanroeper verantwoordelijk voor Dispose (via using)
        }

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            await EnsureAuthHeaderAsync();
            _logger.LogDebug("[ApiService] GET {Endpoint}", endpoint);
            try
            {
                using var res = await SendWithRefreshAsync(() => _client.GetAsync(endpoint));
                _logger.LogDebug("[ApiService] GET {Endpoint} → {Status}", endpoint, (int)res.StatusCode);

                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("[ApiService] GET {Endpoint}: refresh mislukt — token verlopen", endpoint);
                    OnTokenExpired();
                    throw new UnauthorizedAccessException("Token expired or invalid. Please log in again.");
                }

                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync();
                _logger.LogDebug("[ApiService] GET response body ({Len} chars): {Body}",
                    json.Length, Truncate(json, 500));

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("[ApiService] GET {Endpoint} returned empty response body", endpoint);
                    throw new InvalidOperationException("API returned empty response. This may indicate a server error or redirect.");
                }

                if (!IsValidJson(json))
                {
                    _logger.LogError("[ApiService] GET {Endpoint} returned non-JSON response. Body: {Body}",
                        endpoint, Truncate(json, 500));
                    throw new InvalidOperationException($"API returned invalid JSON. Response starts with: {Truncate(json, 100)}. This may indicate a server error page or HTML response.");
                }

                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] GET {Endpoint} failed — HttpRequestException", endpoint);
                throw;
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] GET {Endpoint} failed — unexpected error", endpoint);
                throw;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            await EnsureAuthHeaderAsync();
            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogDebug("[ApiService] POST {Endpoint} | body ({Len} chars): {Body}",
                endpoint, jsonPayload.Length, Truncate(jsonPayload, 500));

            try
            {
                using var res = await SendWithRefreshAsync(() =>
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    return _client.PostAsync(endpoint, content);
                });
                _logger.LogDebug("[ApiService] POST {Endpoint} → {Status}", endpoint, (int)res.StatusCode);

                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("[ApiService] POST {Endpoint}: refresh mislukt — token verlopen", endpoint);
                    OnTokenExpired();
                    throw new UnauthorizedAccessException("Token expired or invalid. Please log in again.");
                }

                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync();
                _logger.LogDebug("[ApiService] POST response body ({Len} chars): {Body}",
                    json.Length, Truncate(json, 500));

                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("API returned empty response.");

                if (!IsValidJson(json))
                    throw new InvalidOperationException($"API returned invalid JSON. Response starts with: {Truncate(json, 100)}.");

                return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] POST {Endpoint} failed — HttpRequestException", endpoint);
                throw;
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] POST {Endpoint} failed — unexpected error", endpoint);
                throw;
            }
        }

        /// <summary>
        /// POST overload die non-success statuscodes niet gooit (voor login/register
        /// waarbij de aanroeper de fout afhandelt). Geen refresh-interceptie: een 401
        /// bij login betekent gewoon verkeerde credentials.
        /// </summary>
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint, TRequest payload, bool allowNonSuccess)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogDebug("[ApiService] POST (tolerant) {Endpoint} | body ({Len} chars): {Body}",
                endpoint, jsonPayload.Length, Truncate(jsonPayload, 500));

            try
            {
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var res = await _client.PostAsync(endpoint, content);

                _logger.LogDebug("[ApiService] POST (tolerant) {Endpoint} → {Status}", endpoint, (int)res.StatusCode);

                if (!res.IsSuccessStatusCode)
                {
                    var errorBody = await res.Content.ReadAsStringAsync();
                    _logger.LogWarning("[ApiService] POST (tolerant) {Endpoint} non-success {Status}. Body: {Body}",
                        endpoint, (int)res.StatusCode, Truncate(errorBody, 300));
                    return default;
                }

                var json = await res.Content.ReadAsStringAsync();
                _logger.LogDebug("[ApiService] POST (tolerant) response body ({Len} chars): {Body}",
                    json.Length, Truncate(json, 500));

                if (string.IsNullOrWhiteSpace(json)) return default;
                if (!IsValidJson(json)) return default;

                return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] POST (tolerant) {Endpoint} failed — HttpRequestException", endpoint);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] POST (tolerant) {Endpoint} failed — unexpected error", endpoint);
                return default;
            }
        }

        public async Task DeleteAsync(string endpoint)
        {
            await EnsureAuthHeaderAsync();
            _logger.LogDebug("[ApiService] DELETE {Endpoint}", endpoint);
            try
            {
                using var res = await SendWithRefreshAsync(() => _client.DeleteAsync(endpoint));
                _logger.LogDebug("[ApiService] DELETE {Endpoint} → {Status}", endpoint, (int)res.StatusCode);

                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("[ApiService] DELETE {Endpoint}: refresh mislukt — token verlopen", endpoint);
                    OnTokenExpired();
                    throw new UnauthorizedAccessException("Token expired or invalid. Please log in again.");
                }

                res.EnsureSuccessStatusCode();
                _logger.LogDebug("[ApiService] DELETE {Endpoint} completed successfully", endpoint);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] DELETE {Endpoint} failed — HttpRequestException", endpoint);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] DELETE {Endpoint} failed — unexpected error", endpoint);
                throw;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void OnTokenExpired() => TokenExpired?.Invoke(this, EventArgs.Empty);

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength] + $"… (+{value.Length - maxLength} chars)";

        private static bool IsValidJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            var trimmed = value.Trim();
            if (!((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
                  (trimmed.StartsWith('[') && trimmed.EndsWith(']')) ||
                  (trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
                  trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                  trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                  trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)))
                return false;

            try { JsonDocument.Parse(value); return true; }
            catch { return false; }
        }

        // ─── Interne DTO voor het refresh-antwoord ────────────────────────────────

        private sealed class RefreshResponse
        {
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
        }
    }
}