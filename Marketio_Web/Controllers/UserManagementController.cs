using Marketio_Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketio_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // ── NIEUW ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.User = user;
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Role {Role} removed from user {Email}", role, user.Email);
                TempData["Success"] = $"Rol '{role}' verwijderd van {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Fout bij het verwijderen van rol.";
            }

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }
        // ──────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId, int lockoutDays = 30)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                _logger.LogWarning("Attempt to lock Admin user {Email} blocked", user.Email);
                TempData["Error"] = "Admin gebruikers kunnen niet worden geblokkeerd voor veiligheidsredenen.";
                return RedirectToAction(nameof(Index));
            }

            var lockoutEnd = DateTimeOffset.UtcNow.AddDays(lockoutDays);
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (result.Succeeded)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Error"] = $"Rol '{role}' bestaat niet.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await _userManager.AddToRoleAsync(user, role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role {Role} assigned to user {Email}", role, user.Email);
                TempData["Success"] = $"Rol '{role}' toegewezen aan {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Fout bij het toewijzen van rol.";
            }

            // Redirect terug naar ManageRoles i.p.v. Index
            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }
    }
}