using Marketio_Shared.Models;
using Marketio_Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Text;

namespace Marketio_Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ConfirmEmailModel(UserManager<AppUser> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            if (userId == null || code == null)
                return RedirectToPage("/Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(_localizer["ConfirmEmail_Error"].Value);

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            IsSuccess = result.Succeeded;
            StatusMessage = result.Succeeded
                ? _localizer["ConfirmEmail_Success"]
                : _localizer["ConfirmEmail_ErrorMessage"];

            return Page();
        }
    }
}