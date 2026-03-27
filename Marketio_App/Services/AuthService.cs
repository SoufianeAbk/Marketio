using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Marketio_Shared.DTOs;
using Microsoft.Maui.Storage;

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
            public string? Message { get; set; }
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

        public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string email, string password)
        {
            var req = new LoginRequest(email, password);
            try
            {
                // Use the tolerant overload that doesn't throw on non-success status codes
                var resp = await _api.PostAsync<LoginRequest, LoginResponse>("api/auth/login", req, allowNonSuccess: true);

                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                {
                    // Extract error message from response if available
                    var errorMsg = resp?.Message ?? "Login failed. Please check your credentials.";
                    return (false, errorMsg);
                }

                await _api.SaveTokenAsync(resp.Token);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(string email, string firstName, string lastName, string? address, string password)
        {
            var req = new RegisterRequest(email, firstName, lastName, address, password);
            try
            {
                // Use the tolerant overload
                var resp = await _api.PostAsync<RegisterRequest, RegisterResponse>("api/auth/register", req, allowNonSuccess: true);

                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                {
                    var errorMsg = resp?.Message ?? "Registration failed. Please try again.";
                    return (false, errorMsg);
                }

                await _api.SaveTokenAsync(resp.Token);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
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
                return await SecureStorage.Default.GetAsync("jwt_token");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates if the stored JWT token is still valid (not expired).
        /// Returns true if token exists and is valid, false otherwise.
        /// </summary>
        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                var handler = new JwtSecurityTokenHandler();

                // Try to parse the JWT
                if (!handler.CanReadToken(token))
                {
                    return false;
                }

                var jwtToken = handler.ReadJwtToken(token);

                // Check expiration time against UTC now
                if (jwtToken.ValidTo <= DateTime.UtcNow)
                {
                    System.Diagnostics.Debug.WriteLine($"[AuthService] JWT token expired at {jwtToken.ValidTo}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[AuthService] JWT token valid until {jwtToken.ValidTo}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Error validating token: {ex.Message}");
                return false;
            }
        }
    }
}