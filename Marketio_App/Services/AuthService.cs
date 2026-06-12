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

        // GDPR-consent meegestuurd bij registratie
        public record RegisterRequest(
            string Email,
            string FirstName,
            string LastName,
            string? Address,
            string Password,
            bool PrivacyConsentGiven,
            bool TermsConsentGiven,
            bool MarketingOptIn);

        private class LoginResponse
        {
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }   // nieuw: refresh token van server
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
            public string? RefreshToken { get; set; }   // nieuw: refresh token van server
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
                // Gebruikt de tolerante overload die geen fout veroorzaakt wanneer een niet-succesvolle statuscode wordt geretourneerd.
                var resp = await _api.PostAsync<LoginRequest, LoginResponse>("api/auth/login", req, allowNonSuccess: true);

                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                {
                    var errorMsg = resp?.Message ?? "Login failed. Please check your credentials.";
                    return (false, errorMsg);
                }

                await _api.SaveTokenAsync(resp.Token);

                // Refresh token opslaan zodat stille vernieuwing mogelijk is
                if (!string.IsNullOrWhiteSpace(resp.RefreshToken))
                    await _api.SaveRefreshTokenAsync(resp.RefreshToken);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        /// <param name="privacyConsentGiven">Gebruiker heeft privacybeleid geaccepteerd.</param>
        /// <param name="termsConsentGiven">Gebruiker heeft algemene voorwaarden geaccepteerd.</param>
        /// <param name="marketingOptIn">Gebruiker heeft toestemming gegeven voor marketing.</param>
        public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(
            string email,
            string firstName,
            string lastName,
            string? address,
            string password,
            bool privacyConsentGiven,
            bool termsConsentGiven,
            bool marketingOptIn)
        {
            var req = new RegisterRequest(
                email,
                firstName,
                lastName,
                address,
                password,
                privacyConsentGiven,
                termsConsentGiven,
                marketingOptIn);

            try
            {
                var resp = await _api.PostAsync<RegisterRequest, RegisterResponse>("api/auth/register", req, allowNonSuccess: true);

                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                {
                    var errorMsg = resp?.Message ?? "Registration failed. Please try again.";
                    return (false, errorMsg);
                }

                await _api.SaveTokenAsync(resp.Token);

                // Refresh token opslaan zodat stille vernieuwing direct na registratie werkt
                if (!string.IsNullOrWhiteSpace(resp.RefreshToken))
                    await _api.SaveRefreshTokenAsync(resp.RefreshToken);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Logt de gebruiker uit: wist JWT + refresh token lokaal.
        /// ClearTokenAsync ruimt beide sleutels op uit SecureStorage.
        /// </summary>
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
        /// Valideert of het opgeslagen JWT-token nog geldig is en niet is verlopen.
        /// Geeft true terug wanneer een geldig token aanwezig is, anders false.
        /// </summary>
        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return false;

                var jwtToken = handler.ReadJwtToken(token);

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
