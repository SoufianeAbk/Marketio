using Marketio_WPF.Models;

namespace Marketio_WPF.Services.Interfaces
{
    /// <summary>
    /// Interface for authentication service operations in WPF application
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Get the currently logged-in user
        /// </summary>
        Task<AppUser?> GetCurrentUserAsync();

        /// <summary>
        /// Get the current user (synchronous property)
        /// </summary>
        AppUser? CurrentUser { get; }

        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="password">User password</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="firstName">User first name</param>
        /// <param name="lastName">User last name</param>
        /// <param name="password">User password</param>
        /// <param name="phoneNumber">User phone number (optional)</param>
        /// <param name="address">User address (optional)</param>
        /// <returns>True if successful, false if registration failed</returns>
        Task<bool> RegisterAsync(
            string email,
            string firstName,
            string lastName,
            string password,
            string? phoneNumber = null,
            string? address = null);

        /// <summary>
        /// Logout the current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        /// <returns>True if user is logged in, false otherwise</returns>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>AppUser if found, null otherwise</returns>
        Task<AppUser?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>AppUser if found, null otherwise</returns>
        Task<AppUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Update user profile information
        /// </summary>
        /// <param name="user">Updated user object</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> UpdateUserAsync(AppUser user);

        /// <summary>
        /// Check if user has specific role
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roleName">Role name to check</param>
        /// <returns>True if user has role, false otherwise</returns>
        Task<bool> UserHasRoleAsync(string userId, string roleName);

        /// <summary>
        /// Get all roles for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of role names</returns>
        Task<IList<string>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Lock user account temporarily (failed login attempts)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="duration">Lock duration in minutes</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> LockUserAsync(string userId, int duration = 30);

        /// <summary>
        /// Unlock user account
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> UnlockUserAsync(string userId);

        /// <summary>
        /// Check if user account is locked
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if locked, false otherwise</returns>
        Task<bool> IsUserLockedAsync(string userId);

        /// <summary>
        /// Deactivate user account (soft delete)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> DeactivateUserAsync(string userId);

        /// <summary>
        /// Reactivate user account
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false if failed</returns>
        Task<bool> ReactivateUserAsync(string userId);
    }
}