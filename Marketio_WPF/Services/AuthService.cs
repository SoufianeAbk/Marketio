using Microsoft.AspNetCore.Identity;
using Marketio_WPF.Models;
using Marketio_WPF.Services.Interfaces;

namespace Marketio_WPF.Services
{
    /// <summary>
    /// Authentication service implementation for WPF application
    /// Handles user authentication, registration, and account management
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private AppUser? _currentUser;

        public AuthService(UserManager<AppUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public Task<AppUser?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public async Task<AppUser?> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return null;
            }

            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return null;
            }

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                // Increment failed login attempts
                await _userManager.AccessFailedAsync(user);
                return null;
            }

            // Reset failed login attempts on successful login
            await _userManager.ResetAccessFailedCountAsync(user);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Set current user
            _currentUser = user;

            return user;
        }

        public async Task<AppUser?> RegisterAsync(
            string email,
            string firstName,
            string lastName,
            string password,
            string? phoneNumber = null,
            string? address = null)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return null;
            }

            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber ?? string.Empty,
                DefaultAddress = address,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(newUser, password);
            if (!result.Succeeded)
            {
                return null;
            }

            // Add user to "User" role
            await _userManager.AddToRoleAsync(newUser, "User");

            return newUser;
        }

        public Task LogoutAsync()
        {
            _currentUser = null;
            return Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

        public bool IsAuthenticated()
        {
            return _currentUser != null && _currentUser.IsActive;
        }

        public async Task<AppUser?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> UpdateUserAsync(AppUser user)
        {
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded && _currentUser?.Id == user.Id)
            {
                _currentUser = user;
            }

            return result.Succeeded;
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<string>();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new List<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> LockUserAsync(string userId, int duration = 30)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var lockoutEndDate = DateTime.UtcNow.AddMinutes(duration);
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

            return result.Succeeded;
        }

        public async Task<bool> UnlockUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            return result.Succeeded;
        }

        public async Task<bool> IsUserLockedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<bool> DeactivateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            // Logout if deactivating current user
            if (_currentUser?.Id == userId)
            {
                _currentUser = null;
            }

            return result.Succeeded;
        }

        public async Task<bool> ReactivateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}