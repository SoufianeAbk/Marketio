using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Marketio_Web.Services
{
    /// <summary>
    /// Singleton in-memory store voor refresh tokens.
    /// Registreer als AddSingleton zodat tokens over requests heen bewaard blijven.
    /// Voor productie: vervang door database-opslag.
    /// </summary>
    public class RefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();

        private record RefreshTokenEntry(string UserId, DateTime Expiry);

        /// <summary>
        /// Maakt een nieuw refresh token aan voor de opgegeven gebruiker.
        /// Eventuele bestaande tokens voor deze gebruiker worden ingetrokken (token-rotatie).
        /// </summary>
        /// <param name="userId">Identity-gebruikers-ID.</param>
        /// <param name="expiryDays">Geldigheid in dagen (standaard 30).</param>
        /// <returns>Het gegenereerde refresh token (opaque, base64).</returns>
        public string Create(string userId, int expiryDays = 30)
        {
            // Token-rotatie: eerder uitgedeeld token voor deze gebruiker intrekken
            RevokeForUser(userId);

            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe base64

            _tokens[token] = new RefreshTokenEntry(userId, DateTime.UtcNow.AddDays(expiryDays));
            return token;
        }

        /// <summary>
        /// Valideert het token en verwijdert het (one-time use).
        /// Retourneert het gebruikers-ID als het token geldig is, anders null.
        /// </summary>
        public (bool IsValid, string? UserId) ValidateAndConsume(string token)
        {
            if (!_tokens.TryRemove(token, out var entry))
                return (false, null);

            if (entry.Expiry < DateTime.UtcNow)
                return (false, null);

            return (true, entry.UserId);
        }

        /// <summary>
        /// Trekt alle tokens voor een gebruiker in (bij uitloggen of wachtwoordwijziging).
        /// </summary>
        public void RevokeForUser(string userId)
        {
            var toRemove = _tokens
                .Where(kvp => kvp.Value.UserId == userId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
                _tokens.TryRemove(key, out _);
        }
    }
}
