using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marketio_Shared.Models;
using Marketio_WPF.Models;

namespace Marketio_WPF.Services
{
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
                        Address = user.Address,
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

        // Enige echte wijziging: ToListAsync() i.p.v. Task.FromResult(...ToList())
        public async Task<List<string>> GetAllRolesAsync()
        {
            try
            {
                return await _roleManager.Roles
                    .Select(r => r.Name ?? string.Empty)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving roles.", ex);
            }
        }
    }
}