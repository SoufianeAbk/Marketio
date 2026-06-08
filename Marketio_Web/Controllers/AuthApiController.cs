using Marketio_Shared.Models;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Marketio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            JwtTokenService jwtTokenService,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>Login endpoint - retourneert JWT-token + refresh token</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request data" });

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.EmailConfirmed)
                return Unauthorized(new { message = "Email not confirmed. Please confirm your email first." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User logged in via API: {Email}", user.Email);

                return Ok(new
                {
                    token,
                    refreshToken,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles,
                    expiresIn = 3600
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out: {Email}", request.Email);
                return Unauthorized(new { message = "Account locked due to multiple failed login attempts" });
            }

            _logger.LogWarning("Login failed: Invalid password - {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        /// <summary>Register endpoint - maakt nieuwe gebruiker aan en retourneert JWT + refresh token</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered" });

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Address = request.Address,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                _logger.LogInformation("New user registered via API: {Email}", user.Email);

                // Auto-confirm e-mail voor API-registraties
                var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, confirmToken);

                var jwtToken = await _jwtTokenService.GenerateTokenAsync(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

                return Ok(new
                {
                    message = "Registration successful",
                    token = jwtToken,
                    refreshToken,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles = new[] { "Customer" },
                    expiresIn = 3600
                });
            }

            return BadRequest(new { message = "Registration failed", errors = result.Errors.Select(e => e.Description) });
        }

        /// <summary>
        /// Vernieuwt het JWT-token via het refresh token (silent re-auth).
        /// Retourneert een nieuw JWT-token én een nieuw refresh token (token-rotatie).
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token is required" });

            var (isValid, userId) = _jwtTokenService.ValidateAndConsumeRefreshToken(request.RefreshToken);

            if (!isValid || userId == null)
            {
                _logger.LogWarning("Refresh token validatie mislukt — token ongeldig of verlopen");
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token verzoek voor onbekende of inactieve gebruiker: {UserId}", userId);
                return Unauthorized(new { message = "User not found or account deactivated" });
            }

            // Genereer nieuw JWT en nieuw refresh token (rotatie)
            var newJwt = await _jwtTokenService.GenerateTokenAsync(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken(userId);

            _logger.LogInformation("Token vernieuwd voor gebruiker: {Email}", user.Email);

            return Ok(new
            {
                token = newJwt,
                refreshToken = newRefreshToken,
                expiresIn = 3600
            });
        }

        /// <summary>
        /// Logt de gebruiker uit door het refresh token in te trekken.
        /// Het JWT blijft technisch geldig tot het verloopt, maar refresh is niet meer mogelijk.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // Verbruik het token zonder het opnieuw uit te geven (intrekken)
                _jwtTokenService.ValidateAndConsumeRefreshToken(request.RefreshToken);
            }

            _logger.LogInformation("Gebruiker uitgelogd via API");
            return Ok(new { message = "Logged out successfully" });
        }
    }

    // ─── Request / Response DTOs ──────────────────────────────────────────────────

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }
}
