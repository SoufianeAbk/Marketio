using Marketio_Shared.Models;

namespace Marketio_WPF.Services.Interfaces
{
    /// <summary>
    /// Interface voor authenticatiediensten in WPF-toepassing
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Haal de momenteel aangemelde gebruiker op
        /// </summary>
        Task<AppUser?> GetCurrentUserAsync();

        /// <summary>
        /// Haal de huidige gebruiker op (synchrone eigenschap)
        /// </summary>
        AppUser? CurrentUser { get; }

        /// <summary>
        /// Authenticeer een gebruiker met e-mailadres en wachtwoord
        /// </summary>
        /// <param name="email">E-mailadres van de gebruiker</param>
        /// <param name="password">Wachtwoord van de gebruiker</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// Registreer een nieuwe gebruiker
        /// </summary>
        /// <param name="email">E-mailadres van de gebruiker</param>
        /// <param name="firstName">Voornaam van de gebruiker</param>
        /// <param name="lastName">Achternaam van de gebruiker</param>
        /// <param name="password">Wachtwoord van de gebruiker</param>
        /// <param name="phoneNumber">Telefoonnummer van de gebruiker (optioneel)</param>
        /// <param name="address">Adres van de gebruiker (optioneel)</param>
        /// <returns>True indien succesvol, anders false indien registratie mislukt</returns>
        Task<bool> RegisterAsync(
            string email,
            string firstName,
            string lastName,
            string password,
            string? phoneNumber = null,
            string? address = null);

        /// <summary>
        /// Meld de huidige gebruiker af
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Wijzig het wachtwoord van een geauthenticeerde gebruiker
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <param name="currentPassword">Huidig wachtwoord</param>
        /// <param name="newPassword">Nieuw wachtwoord</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        /// <summary>
        /// Controleer of de gebruiker geauthenticeerd is
        /// </summary>
        /// <returns>True indien de gebruiker aangemeld is, anders false</returns>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Haal een gebruiker op via ID
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>AppUser indien gevonden, anders null</returns>
        Task<AppUser?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Haalt een gebruiker op via e-mailadres
        /// </summary>
        /// <param name="email">E-mailadres van de gebruiker</param>
        /// <returns>AppUser indien gevonden, anders null</returns>
        Task<AppUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Werk profielinformatie van een gebruiker bij
        /// </summary>
        /// <param name="user">Bijgewerkt gebruikersobject</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> UpdateUserAsync(AppUser user);

        /// <summary>
        /// Controleer of een gebruiker een specifieke rol heeft
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <param name="roleName">Te controleren rolnaam</param>
        /// <returns>True indien de gebruiker de rol heeft, anders false</returns>
        Task<bool> UserHasRoleAsync(string userId, string roleName);

        /// <summary>
        /// Haalt alle rollen van een gebruiker op
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>Lijst met rolnamen</returns>
        Task<IList<string>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Vergrendelt tijdelijk een gebruikersaccount (mislukte aanmeldpogingen)
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <param name="duration">Duur van de vergrendeling in minuten</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> LockUserAsync(string userId, int duration = 30);

        /// <summary>
        /// Ontgrendelt een gebruikersaccount
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> UnlockUserAsync(string userId);

        /// <summary>
        /// Controleert of een gebruikersaccount vergrendeld is
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>True indien vergrendeld, anders false</returns>
        Task<bool> IsUserLockedAsync(string userId);

        /// <summary>
        /// Deactiveert een gebruikersaccount (soft delete)
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> DeactivateUserAsync(string userId);

        /// <summary>
        /// Heractiveer een gebruikersaccount
        /// </summary>
        /// <param name="userId">Gebruikers-ID</param>
        /// <returns>True indien succesvol, anders false</returns>
        Task<bool> ReactivateUserAsync(string userId);
    }
}