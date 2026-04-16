using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace Marketio_App.Services
{
    /// <summary>
    /// Handles secure generation, storage, and retrieval of database encryption keys.
    /// Complies with GDPR and platform security best practices.
    /// 
    /// Strategy:
    /// - DEBUG: Generates a persistent key on first run (for development consistency)
    /// - RELEASE: Uses device-native secure storage (Keychain, Keystore)
    /// </summary>
    public class SecureKeyManagementService
    {
        private const string DbEncryptionKeyId = "marketio_db_encryption_key_v1";
        private const string KeyDerivationSalt = "marketio_salt_v1";
        private const int KeySizeBytes = 32; // 256-bit key for AES

        /// <summary>
        /// Gets or generates the database encryption key securely.
        /// </summary>
        public async Task<string> GetOrCreateDatabaseKeyAsync()
        {
            try
            {
                // Attempt to retrieve existing key from secure storage
                var existingKey = await SecureStorage.Default.GetAsync(DbEncryptionKeyId);
                if (!string.IsNullOrEmpty(existingKey))
                {
                    return existingKey;
                }

                // Generate new key if none exists
                var newKey = GenerateSecureKey();
                await SecureStorage.Default.SetAsync(DbEncryptionKeyId, newKey);

                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] New database encryption key generated and stored.");
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Error in GetOrCreateDatabaseKeyAsync: {ex.Message}");
                throw new InvalidOperationException("Failed to retrieve or create database encryption key.", ex);
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random key.
        /// </summary>
        private static string GenerateSecureKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[KeySizeBytes];
                rng.GetBytes(keyBytes);

                // Return as Base64 for safe storage
                return Convert.ToBase64String(keyBytes);
            }
        }

        /// <summary>
        /// Derives a key from a password using PBKDF2 (for fallback scenarios).
        /// NOT used for primary encryption - only for backup key derivation.
        /// </summary>
        public static string DeriveKeyFromPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            var salt = Encoding.UTF8.GetBytes(KeyDerivationSalt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations: 10000, hashAlgorithm: HashAlgorithmName.SHA256))
            {
                var derivedKey = pbkdf2.GetBytes(KeySizeBytes);
                return Convert.ToBase64String(derivedKey);
            }
        }

        /// <summary>
        /// Clears the stored encryption key (use during account deletion/logout).
        /// </summary>
        public async Task ClearDatabaseKeyAsync()
        {
            try
            {
                SecureStorage.Default.Remove(DbEncryptionKeyId);
                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] Database encryption key cleared.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Error clearing database key: {ex.Message}");
            }
        }

        /// <summary>
        /// Rotates the encryption key (advanced GDPR compliance feature).
        /// </summary>
        public async Task<string> RotateKeyAsync()
        {
            try
            {
                // Clear old key
                await ClearDatabaseKeyAsync();

                // Generate and store new key
                var newKey = GenerateSecureKey();
                await SecureStorage.Default.SetAsync(DbEncryptionKeyId, newKey);

                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] Encryption key rotated successfully.");
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Error rotating encryption key: {ex.Message}");
                throw;
            }
        }
    }
}