using Microsoft.AspNetCore.Identity;
using Marketio_Shared.Models;
using Marketio_WPF.Models;

namespace Marketio_WPF.Services
{
    /// <summary>
    /// Service for managing user administration operations.
    /// Handles user listing, role assignment, and user account operations.
    /// </summary>
    internal class UserManagementService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        /// <summary>
        /// Retrieves all users from the system as typed DTOs.
        /// Typed return type avoids read-only anonymous-type properties
        /// that cause TwoWay binding crashes in WPF DataGridCheckBoxColumns.
        /// </summary>
        public async Task<List<UserAdminDto>> GetAllUsersAsync()
        {
            try
            {
                var users = _userManager.Users.ToList();
                var result = new List<UserAdminDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var isLocked = await _userManager.IsLockedOutAsync(user);

                    result.Add(new UserAdminDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        FullName = user.FullName ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.DefaultAddress,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        IsActive = user.IsActive,
                        IsLocked = isLocked,
                        Roles = string.Join(", ", roles),
                        EmailConfirmed = user.EmailConfirmed
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving users.", ex);
            }
        }

        /// <summary>Assigns a role to a user.</summary>
        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                if (!await _roleManager.RoleExistsAsync(roleName)) return false;
                if (await _userManager.IsInRoleAsync(user, roleName)) return false;

                var result = await _userManager.AddToRoleAsync(user, roleName);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error assigning role '{roleName}' to user.", ex);
            }
        }

        /// <summary>Removes a role from a user.</summary>
        public async Task<bool> RemoveRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                if (!await _userManager.IsInRoleAsync(user, roleName)) return false;

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error removing role '{roleName}' from user.", ex);
            }
        }

        /// <summary>Generates a password reset token for a user.</summary>
        public async Task<bool> ResetPasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                return !string.IsNullOrEmpty(resetToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error resetting password.", ex);
            }
        }

        /// <summary>Locks a user account for 10 years (effectively permanent).</summary>
        public async Task<bool> LockUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.SetLockoutEndDateAsync(
                    user, DateTimeOffset.UtcNow.AddYears(10));

                if (result.Succeeded)
                    await _userManager.SetLockoutEnabledAsync(user, true);

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error locking user account.", ex);
            }
        }

        /// <summary>Permanently deletes a user (GDPR right to be forgotten).</summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting user.", ex);
            }
        }

        /// <summary>Gets all available role names in the system.</summary>
        public async Task<List<string>> GetAllRolesAsync()
        {
            try
            {
                return await Task.FromResult(
                    _roleManager.Roles
                                .Select(r => r.Name ?? string.Empty)
                                .ToList());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving roles.", ex);
            }
        }
    }
}
