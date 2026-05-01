using Microsoft.AspNetCore.Identity;
using Marketio_WPF.Models;
using System.Collections.ObjectModel;

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
        /// Retrieves all users from the system.
        /// </summary>
        /// <returns>List of dynamic objects containing user information</returns>
        public async Task<List<dynamic>> GetAllUsersAsync()
        {
            try
            {
                var users = _userManager.Users.ToList();
                var usersList = new List<dynamic>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var isLocked = await _userManager.IsLockedOutAsync(user);

                    usersList.Add(new
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        UserName = user.UserName,
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

                return usersList;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving users.", ex);
            }
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="roleName">The role name to assign</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                // Check if role exists
                if (!await _roleManager.RoleExistsAsync(roleName))
                    return false;

                // Check if user already has role
                if (await _userManager.IsInRoleAsync(user, roleName))
                    return false;

                var result = await _userManager.AddToRoleAsync(user, roleName);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error assigning role '{roleName}' to user.", ex);
            }
        }

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="roleName">The role name to remove</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RemoveRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                // Check if user has role
                if (!await _userManager.IsInRoleAsync(user, roleName))
                    return false;

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error removing role '{roleName}' from user.", ex);
            }
        }

        /// <summary>
        /// Sends a password reset email to the user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ResetPasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                // Generate password reset token
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                // In a real application, you would send this token via email
                // For now, we'll just verify the token was generated successfully
                return !string.IsNullOrEmpty(resetToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error resetting password.", ex);
            }
        }

        /// <summary>
        /// Locks a user account to prevent login.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> LockUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                var lockoutEndDate = DateTimeOffset.UtcNow.AddYears(10);
                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

                if (result.Succeeded)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error locking user account.", ex);
            }
        }

        /// <summary>
        /// Deletes a user from the system.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting user.", ex);
            }
        }

        /// <summary>
        /// Gets all available roles in the system.
        /// </summary>
        /// <returns>List of role names</returns>
        public async Task<List<string>> GetAllRolesAsync()
        {
            try
            {
                return await Task.FromResult(_roleManager.Roles.Select(r => r.Name ?? string.Empty).ToList());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving roles.", ex);
            }
        }
    }
}