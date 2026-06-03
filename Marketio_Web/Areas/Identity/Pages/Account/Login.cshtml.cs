using Marketio_Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Marketio_Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is verplicht")]
            [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Wachtwoord is verplicht")]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Onthoud mij")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");

            // Wis bestaande externe cookie om een schone login te garanderen
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Bijwerken LastLoginAt
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogInformation("Gebruiker {Email} is ingelogd.", Input.Email);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Gebruikersaccount {Email} is vergrendeld.", Input.Email);
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Ongeldige inlogpoging.");
            return Page();
        }
    }
}
