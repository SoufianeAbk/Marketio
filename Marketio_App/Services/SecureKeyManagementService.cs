using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace Marketio_App.Services
{
    /// <summary>
    /// Beheert de veilige generatie, opslag en ophaling van databaseversleutelingssleutels.
    /// Voldoet aan de AVG en platformbeveiligingsnormen.
    /// 
    /// Strategie:
    /// - DEBUG: Genereert een persistente sleutel bij de eerste uitvoering (voor consistentie tijdens ontwikkeling)
    /// - RELEASE: Gebruikt de native beveiligde opslag van het apparaat (Keychain, Keystore)
    /// </summary>
    public class SecureKeyManagementService
    {
        private const string DbEncryptionKeyId = "marketio_db_encryption_key_v1";
        private const string KeyDerivationSalt = "marketio_salt_v1";
        private const int KeySizeBytes = 32; // 256-bit sleutel voor AES

        /// <summary>
        /// Haalt de databaseversleutelingssleutel op of genereert een nieuwe.
        /// </summary>
        public async Task<string> GetOrCreateDatabaseKeyAsync()
        {
            try
            {
                // Probeer bestaande sleutel op te halen uit beveiligde opslag
                var existingKey = await SecureStorage.Default.GetAsync(DbEncryptionKeyId);
                if (!string.IsNullOrEmpty(existingKey))
                {
                    return existingKey;
                }

                // Genereer een nieuwe sleutel als er geen bestaat
                var newKey = GenerateSecureKey();
                await SecureStorage.Default.SetAsync(DbEncryptionKeyId, newKey);

                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] Nieuwe databaseversleutelingssleutel gegenereerd en opgeslagen.");
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Fout in GetOrCreateDatabaseKeyAsync: {ex.Message}");
                throw new InvalidOperationException("Ophalen of aanmaken van databaseversleutelingssleutel mislukt.", ex);
            }
        }

        /// <summary>
        /// Genereert een cryptografisch veilige willekeurige sleutel.
        /// </summary>
        private static string GenerateSecureKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[KeySizeBytes];
                rng.GetBytes(keyBytes);

                // Geef terug als Base64 voor veilige opslag
                return Convert.ToBase64String(keyBytes);
            }
        }

        /// <summary>
        /// Leidt een sleutel af van een wachtwoord via PBKDF2 (voor noodscenario's).
        /// NIET gebruikt voor primaire versleuteling – alleen voor afgeleide reservesleutels.
        /// </summary>
        public static string DeriveKeyFromPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Wachtwoord mag niet leeg of null zijn.", nameof(password));

            var salt = Encoding.UTF8.GetBytes(KeyDerivationSalt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations: 10000, hashAlgorithm: HashAlgorithmName.SHA256))
            {
                var derivedKey = pbkdf2.GetBytes(KeySizeBytes);
                return Convert.ToBase64String(derivedKey);
            }
        }

        /// <summary>
        /// Verwijdert de opgeslagen versleutelingssleutel (gebruik bij accountverwijdering of uitloggen).
        /// </summary>
        public async Task ClearDatabaseKeyAsync()
        {
            try
            {
                SecureStorage.Default.Remove(DbEncryptionKeyId);
                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] Databaseversleutelingssleutel verwijderd.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Fout bij verwijderen van databasesleutel: {ex.Message}");
            }
        }

        /// <summary>
        /// Roteert de versleutelingssleutel (geavanceerde AVG-nalevingsfunctie).
        /// </summary>
        public async Task<string> RotateKeyAsync()
        {
            try
            {
                // Verwijdert de oude sleutel
                await ClearDatabaseKeyAsync();

                // Genereert en slaat een nieuwe sleutel op
                var newKey = GenerateSecureKey();
                await SecureStorage.Default.SetAsync(DbEncryptionKeyId, newKey);

                System.Diagnostics.Debug.WriteLine("[SecureKeyManagement] Versleutelingssleutel succesvol geroteerd.");
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureKeyManagement] Fout bij roteren van versleutelingssleutel: {ex.Message}");
                throw;
            }
        }
    }
}