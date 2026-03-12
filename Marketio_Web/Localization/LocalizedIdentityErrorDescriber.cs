using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Marketio_Web.Localization
{
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
        }

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = _localizer["IdentityError_DuplicateEmail", email]
            };
        }

        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = _localizer["IdentityError_DuplicateUserName", userName]
            };
        }

        public override IdentityError InvalidEmail(string? email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = _localizer["IdentityError_InvalidEmail", email ?? ""]
            };
        }

        public override IdentityError DuplicateRoleName(string role)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                Description = _localizer["IdentityError_DuplicateRoleName", role]
            };
        }

        public override IdentityError InvalidRoleName(string? role)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                Description = _localizer["IdentityError_InvalidRoleName", role ?? ""]
            };
        }

        public override IdentityError InvalidToken()
        {
            return new IdentityError
            {
                Code = nameof(InvalidToken),
                Description = _localizer["IdentityError_InvalidToken"]
            };
        }

        public override IdentityError InvalidUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = _localizer["IdentityError_InvalidUserName", userName ?? ""]
            };
        }

        public override IdentityError LoginAlreadyAssociated()
        {
            return new IdentityError
            {
                Code = nameof(LoginAlreadyAssociated),
                Description = _localizer["IdentityError_LoginAlreadyAssociated"]
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = _localizer["IdentityError_PasswordMismatch"]
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = _localizer["IdentityError_PasswordRequiresDigit"]
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = _localizer["IdentityError_PasswordRequiresLower"]
            };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = _localizer["IdentityError_PasswordRequiresNonAlphanumeric"]
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = _localizer["IdentityError_PasswordRequiresUpper"]
            };
        }

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUniqueChars),
                Description = _localizer["IdentityError_PasswordRequiresUniqueChars", uniqueChars]
            };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = _localizer["IdentityError_PasswordTooShort", length]
            };
        }

        public override IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyHasPassword),
                Description = _localizer["IdentityError_UserAlreadyHasPassword"]
            };
        }

        public override IdentityError UserAlreadyInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyInRole),
                Description = _localizer["IdentityError_UserAlreadyInRole", role]
            };
        }

        public override IdentityError UserNotInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserNotInRole),
                Description = _localizer["IdentityError_UserNotInRole", role]
            };
        }

        public override IdentityError UserLockoutNotEnabled()
        {
            return new IdentityError
            {
                Code = nameof(UserLockoutNotEnabled),
                Description = _localizer["IdentityError_UserLockoutNotEnabled"]
            };
        }

        public override IdentityError RecoveryCodeRedemptionFailed()
        {
            return new IdentityError
            {
                Code = nameof(RecoveryCodeRedemptionFailed),
                Description = _localizer["IdentityError_RecoveryCodeRedemptionFailed"]
            };
        }

        public override IdentityError ConcurrencyFailure()
        {
            return new IdentityError
            {
                Code = nameof(ConcurrencyFailure),
                Description = _localizer["IdentityError_ConcurrencyFailure"]
            };
        }

        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description = _localizer["IdentityError_DefaultError"]
            };
        }
    }
}