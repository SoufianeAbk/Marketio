using Marketio_Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Marketio_Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // ── Weergave-eigenschappen (readonly) ──────────────────────────────

        public string Username { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        // Extra AppUser-velden (readonly, getoond in kaart "Accountinformatie")
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        // GDPR (readonly badges, behalve MarketingOptIn)
        public bool PrivacyConsentGiven { get; set; }
        public bool TermsConsentGiven { get; set; }
        public bool MarketingOptIn { get; set; }
        public DateTime? ConsentGivenDate { get; set; }

        // ── Bewerkbaar formulier ───────────────────────────────────────────

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Voornaam is verplicht")]
            [StringLength(100, ErrorMessage = "Voornaam mag maximaal 100 tekens zijn")]
            [Display(Name = "Voornaam")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Achternaam is verplicht")]
            [StringLength(100, ErrorMessage = "Achternaam mag maximaal 100 tekens zijn")]
            [Display(Name = "Achternaam")]
            public string LastName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Ongeldig telefoonnummer")]
            [Display(Name = "Telefoonnummer")]
            public string? PhoneNumber { get; set; }

            [StringLength(500, ErrorMessage = "Adres mag maximaal 500 tekens zijn")]
            [Display(Name = "Adres")]
            public string? Address { get; set; }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private async Task LoadAsync(AppUser user)
        {
            Username = user.Email ?? string.Empty;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
            };

            CreatedAt = user.CreatedAt;
            LastLoginAt = user.LastLoginAt;
            IsActive = user.IsActive;
            PrivacyConsentGiven = user.PrivacyConsentGiven;
            TermsConsentGiven = user.TermsConsentGiven;
            MarketingOptIn = user.MarketingOptIn;
            ConsentGivenDate = user.ConsentGivenDate;
        }

        // ── GET ───────────────────────────────────────────────────────────

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Kan gebruiker met ID '{_userManager.GetUserId(User)}' niet laden.");

            await LoadAsync(user);
            return Page();
        }

        // ── POST: profielgegevens opslaan ─────────────────────────────────

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Kan gebruiker met ID '{_userManager.GetUserId(User)}' niet laden.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var changed = false;

            if (user.FirstName != Input.FirstName)
            {
                user.FirstName = Input.FirstName;
                changed = true;
            }

            if (user.LastName != Input.LastName)
            {
                user.LastName = Input.LastName;
                changed = true;
            }

            if (user.Address != Input.Address)
            {
                user.Address = Input.Address;
                changed = true;
            }

            if (user.PhoneNumber != Input.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Fout bij het opslaan van het telefoonnummer.";
                    return RedirectToPage();
                }
            }

            if (changed)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Fout bij het opslaan van de profielgegevens.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Gebruiker {UserId} heeft zijn profiel bijgewerkt.", user.Id);
            StatusMessage = "Uw profiel is bijgewerkt.";
            return RedirectToPage();
        }

        // ── POST handler: marketing opt-in/out wisselen ───────────────────

        public async Task<IActionResult> OnPostToggleMarketingAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            user.MarketingOptIn = !user.MarketingOptIn;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Fout bij het wijzigen van de marketingvoorkeur.";
                return RedirectToPage();
            }

            StatusMessage = user.MarketingOptIn
                ? "U bent ingeschreven voor marketingberichten."
                : "U bent uitgeschreven van marketingberichten.";

            return RedirectToPage();
        }
    }
}
