using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Marketio_App.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string JwtKey = "jwt_token";

        public ApiService(HttpClient httpClient)
        {
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task InitializeAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(JwtKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    SetAuthorizationHeader(token);
                }
            }
            catch
            {
                // SecureStorage may throw on some platforms/emulators - ignore during init
            }
        }

        public void SetAuthorizationHeader(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task SaveTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            try
            {
                await SecureStorage.Default.SetAsync(JwtKey, token);
                SetAuthorizationHeader(token);
            }
            catch
            {
                // Swallow - if SecureStorage fails, header still set for current session
                SetAuthorizationHeader(token);
            }
        }

        public async Task ClearTokenAsync()
        {
            try
            {
                // SecureStorage.Remove is synchronous on MAUI's ISecureStorage
                SecureStorage.Default.Remove(JwtKey);
            }
            catch
            {
                // ignore
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            using var res = await _client.GetAsync(endpoint);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var res = await _client.PostAsync(endpoint, content);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, bool allowNotFoundOrBadRequest)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var res = await _client.PostAsync(endpoint, content);

            if (!res.IsSuccessStatusCode)
            {
                // return default so caller can handle non-success responses gracefully
                return default;
            }

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
        }
    }
}