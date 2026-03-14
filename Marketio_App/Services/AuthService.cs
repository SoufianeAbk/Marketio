using System;
using System.Threading.Tasks;
using Marketio_Shared.DTOs;

namespace Marketio_App.Services
{
    public class AuthService
    {
        private readonly ApiService _api;

        public AuthService(ApiService api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public record LoginRequest(string Email, string Password);
        public record RegisterRequest(string Email, string FirstName, string LastName, string? Address, string Password);

        private class LoginResponse
        {
            public string? Token { get; set; }
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string[]? Roles { get; set; }
            public int ExpiresIn { get; set; }
        }

        private class RegisterResponse
        {
            public string? Message { get; set; }
            public string? Token { get; set; }
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string[]? Roles { get; set; }
            public int ExpiresIn { get; set; }
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var req = new LoginRequest(email, password);
            try
            {
                var resp = await _api.PostAsync<LoginRequest, LoginResponse>("api/auth/login", req);
                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                    return false;

                await _api.SaveTokenAsync(resp.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string email, string firstName, string lastName, string? address, string password)
        {
            var req = new RegisterRequest(email, firstName, lastName, address, password);
            try
            {
                var resp = await _api.PostAsync<RegisterRequest, RegisterResponse>("api/auth/register", req);
                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                    return false;

                await _api.SaveTokenAsync(resp.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task LogoutAsync()
        {
            return _api.ClearTokenAsync();
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("jwt_token");
            }
            catch
            {
                return null;
            }
        }
    }
}