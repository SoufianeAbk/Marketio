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
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
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
    }
}