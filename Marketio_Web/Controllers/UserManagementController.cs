using Marketio_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketio_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // POST: UserManagement/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId, int lockoutDays = 30)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            //  Voorkomt blokkeren van Admin gebruikers
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                _logger.LogWarning("Attempt to lock Admin user {Email} blocked", user.Email);
                TempData["Error"] = "Admin gebruikers kunnen niet worden geblokkeerd voor veiligheidsredenen.";
                return RedirectToAction(nameof(Index));
            }

            // Set lockout end date
            var lockoutEnd = DateTimeOffset.UtcNow.AddDays(lockoutDays);
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (result.Succeeded)
            {
                // Enable lockout
                await _userManager.SetLockoutEnabledAsync(user, true);
                _logger.LogWarning("User {Email} has been locked out until {LockoutEnd}", user.Email, lockoutEnd);
                TempData["Success"] = $"Gebruiker {user.Email} is geblokkeerd tot {lockoutEnd:dd-MM-yyyy HH:mm}";
            }
            else
            {
                TempData["Error"] = "Fout bij het blokkeren van gebruiker.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: UserManagement/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Remove lockout
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
                // Reset failed access count
                await _userManager.ResetAccessFailedCountAsync(user);
                _logger.LogInformation("User {Email} has been unlocked", user.Email);
                TempData["Success"] = $"Gebruiker {user.Email} is gedeblokkeerd.";
            }
            else
            {
                TempData["Error"] = "Fout bij het deblokkeren van gebruiker.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/Details
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        // GET: UserManagement/ManageRoles
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            ViewBag.User = user;
            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = allRoles;

            return View();
        }

        // POST: UserManagement/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Checkt of de rol bestaat
            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Error"] = $"Rol '{role}' bestaat niet.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            // Checkt of de user de rol al heeft
            if (await _userManager.IsInRoleAsync(user, role))
            {
                TempData["Warning"] = $"Gebruiker {user.Email} heeft al de rol '{role}'.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {Role} assigned to user {Email}", role, user.Email);
                TempData["Success"] = $"Rol '{role}' is toegewezen aan {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Fout bij het toewijzen van de rol.";
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Error assigning role: {Error}", error.Description);
                }
            }

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // POST: UserManagement/RemoveRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Checkt of de user de rollen heeft
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                TempData["Warning"] = $"Gebruiker {user.Email} heeft de rol '{role}' niet.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {Role} removed from user {Email}", role, user.Email);
                TempData["Success"] = $"Rol '{role}' is verwijderd van {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Fout bij het verwijderen van de rol.";
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Error removing role: {Error}", error.Description);
                }
            }

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }
    }
}