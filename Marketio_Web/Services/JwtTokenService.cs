using Marketio_Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Marketio_Web.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;
        private readonly RefreshTokenStore _refreshTokenStore;

        public JwtTokenService(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            RefreshTokenStore refreshTokenStore)
        {
            _configuration = configuration;
            _userManager = userManager;
            _refreshTokenStore = refreshTokenStore;
        }

        // ─── Access token (JWT) ───────────────────────────────────────────────────

        public async Task<string> GenerateTokenAsync(AppUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ─── Refresh token ────────────────────────────────────────────────────────

        /// <summary>
        /// Genereert een nieuw refresh token voor de gebruiker.
        /// Eventuele bestaande tokens voor deze gebruiker worden ingetrokken.
        /// </summary>
        public string GenerateRefreshToken(string userId, int expiryDays = 30)
            => _refreshTokenStore.Create(userId, expiryDays);

        /// <summary>
        /// Valideert en verbruikt het refresh token (one-time use).
        /// </summary>
        public (bool IsValid, string? UserId) ValidateAndConsumeRefreshToken(string refreshToken)
            => _refreshTokenStore.ValidateAndConsume(refreshToken);

        /// <summary>
        /// Trekt alle refresh tokens voor een gebruiker in (bij uitloggen).
        /// </summary>
        public void RevokeRefreshTokens(string userId)
            => _refreshTokenStore.RevokeForUser(userId);
    }
}
