using Marketio_Web.Models;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace Marketio_Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IGdprAuditService _gdprAuditService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IGdprAuditService gdprAuditService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _gdprAuditService = gdprAuditService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Email is verplicht")]
            [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Voornaam is verplicht")]
            [StringLength(100, ErrorMessage = "Voornaam mag maximaal 100 tekens zijn")]
            [Display(Name = "Voornaam")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Achternaam is verplicht")]
            [StringLength(100, ErrorMessage = "Achternaam mag maximaal 100 tekens zijn")]
            [Display(Name = "Achternaam")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "Adres mag maximaal 500 tekens zijn")]
            [Display(Name = "Adres")]
            public string? Address { get; set; }

            [Required(ErrorMessage = "Wachtwoord is verplicht")]
            [StringLength(100, ErrorMessage = "{0} moet minstens {2} en maximaal {1} tekens lang zijn.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Bevestig wachtwoord")]
            [Compare("Password", ErrorMessage = "Het wachtwoord en bevestigingswachtwoord komen niet overeen.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "U moet de voorwaarden accepteren")]
            [Display(Name = "Ik accepteer de voorwaarden")]
            public bool TermsConsentGiven { get; set; }

            [Required(ErrorMessage = "U moet het privacybeleid accepteren")]
            [Display(Name = "Ik accepteer het privacybeleid")]
            public bool PrivacyConsentGiven { get; set; }

            [Display(Name = "Ik wil graag aanbiedingen ontvangen")]
            public bool MarketingOptIn { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Address = Input.Address ?? string.Empty,
                    // GDPR Consent
                    TermsConsentGiven = Input.TermsConsentGiven,
                    PrivacyConsentGiven = Input.PrivacyConsentGiven,
                    MarketingOptIn = Input.MarketingOptIn,
                    ConsentGivenDate = DateTime.UtcNow
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "User created a new account with password. Email: {Email} | Terms Accepted: {TermsAccepted} | Privacy Accepted: {PrivacyAccepted} | Marketing: {Marketing}",
                        Input.Email,
                        Input.TermsConsentGiven,
                        Input.PrivacyConsentGiven,
                        Input.MarketingOptIn
                    );

                    // Assign Customer role
                    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Customer role assigned to user {Email}", Input.Email);
                    }

                    // GDPR: Log consent events in audit trail
                    var userId = await _userManager.GetUserIdAsync(user);
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                    try
                    {
                        await _gdprAuditService.LogConsentEventAsync(
                            userId,
                            "terms",
                            Input.TermsConsentGiven,
                            ipAddress,
                            userAgent,
                            $"Toestemming gegeven bij registratie op {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss} UTC");

                        await _gdprAuditService.LogConsentEventAsync(
                            userId,
                            "privacy",
                            Input.PrivacyConsentGiven,
                            ipAddress,
                            userAgent,
                            $"Toestemming gegeven bij registratie op {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss} UTC");

                        await _gdprAuditService.LogConsentEventAsync(
                            userId,
                            "marketing",
                            Input.MarketingOptIn,
                            ipAddress,
                            userAgent,
                            $"Marketing opt-{(Input.MarketingOptIn ? "in" : "out")} bij registratie op {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss} UTC");

                        _logger.LogInformation(
                            "GDPR audit logs aangemaakt voor gebruiker {UserId} bij registratie.",
                            userId);
                    }
                    catch (Exception ex)
                    {
                        // Audit logging mag registratie niet blokkeren — enkel loggen
                        _logger.LogError(ex,
                            "Fout bij aanmaken GDPR audit logs voor gebruiker {UserId}. Registratie gaat door.",
                            userId);
                    }

                    // Generate email confirmation token
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    // Send confirmation email
                    await _emailSender.SendEmailAsync(Input.Email, "Bevestig uw email",
                        $"Bevestig uw account door <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>hier te klikken</a>.");

                    // Redirect to confirmation page
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}