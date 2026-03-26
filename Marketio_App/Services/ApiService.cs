using System;
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

        /// <summary>
        /// Exposes the base API URL for constructing absolute URLs (e.g., for images)
        /// </summary>
        public Uri? BaseAddress => _client.BaseAddress;

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

        public async Task ClearTokenAsync()
        {
            _logger.LogDebug("[ApiService] ClearTokenAsync called.");
            try
            {
                SecureStorage.Default.Remove(JwtKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ApiService] SecureStorage.Remove failed — continuing.");
            }
            _client.DefaultRequestHeaders.Authorization = null;
            await Task.CompletedTask; // keep async signature consistent
        }

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            _logger.LogDebug("[ApiService] GET {Endpoint}", endpoint);
            try
            {
                using var res = await _client.GetAsync(endpoint);
                _logger.LogDebug("[ApiService] GET {Endpoint} → {Status}", endpoint, (int)res.StatusCode);
                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync();
                _logger.LogDebug("[ApiService] GET response body ({Len} chars): {Body}",
                    json.Length, Truncate(json, 500));

                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] GET {Endpoint} failed — HttpRequestException", endpoint);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] GET {Endpoint} failed — unexpected error", endpoint);
                throw;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogDebug("[ApiService] POST {Endpoint} | body ({Len} chars): {Body}",
                endpoint, jsonPayload.Length, Truncate(jsonPayload, 500));

            try
            {
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var res = await _client.PostAsync(endpoint, content);
                _logger.LogDebug("[ApiService] POST {Endpoint} → {Status}", endpoint, (int)res.StatusCode);
                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync();
                _logger.LogDebug("[ApiService] POST response body ({Len} chars): {Body}",
                    json.Length, Truncate(json, 500));

                return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] POST {Endpoint} failed — HttpRequestException", endpoint);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApiService] POST {Endpoint} failed — unexpected error", endpoint);
                throw;
            }
        }

        /// <summary>
        /// POST overload that swallows non-success status codes and returns default
        /// instead of throwing (useful for login/register where the caller handles errors).
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

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength] + $"… (+{value.Length - maxLength} chars)";
    }
}