using Microsoft.AspNetCore.Identity;
using Marketio_Shared.Models;
using Marketio_WPF.Services.Interfaces;

namespace Marketio_WPF.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private AppUser? _currentUser;

        public AppUser? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null && _currentUser.IsActive;

        public AuthService(UserManager<AppUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public Task<AppUser?> GetCurrentUserAsync() => Task.FromResult(_currentUser);

        public async Task<bool> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive) return false;

            if (await _userManager.IsLockedOutAsync(user)) return false;

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                return false;
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _currentUser = user;
            return true;
        }

        public async Task<bool> RegisterAsync(
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
                return false;

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null) return false;

            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber ?? string.Empty,
                Address = address,           // <-- was DefaultAddress
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(newUser, password);
            if (!result.Succeeded) return false;

            await _userManager.AddToRoleAsync(newUser, "User");
            return true;
        }

        public Task LogoutAsync()
        {
            _currentUser = null;
            return Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<AppUser?> GetUserByIdAsync(string userId)
            => string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);

        public async Task<AppUser?> GetUserByEmailAsync(string email)
            => string.IsNullOrWhiteSpace(email) ? null : await _userManager.FindByEmailAsync(email);

        public async Task<bool> UpdateUserAsync(AppUser user)
        {
            if (user == null) return false;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded && _currentUser?.Id == user.Id)
                _currentUser = user;

            return result.Succeeded;
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<string>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> LockUserAsync(string userId, int duration = 30)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddMinutes(duration));
            return result.Succeeded;
        }

        public async Task<bool> UnlockUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            return result.Succeeded;
        }

        public async Task<bool> IsUserLockedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<bool> DeactivateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (_currentUser?.Id == userId)
                _currentUser = null;

            return result.Succeeded;
        }

        public async Task<bool> ReactivateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}
