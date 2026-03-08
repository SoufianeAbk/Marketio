using Marketio_Web.Models;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtTokenService jwtTokenService,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint - returns JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check of email is bevestigd
            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Email not confirmed. Please confirm your email first." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User logged in via API: {Email}", user.Email);

                return Ok(new
                {
                    token = token,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles = roles,
                    expiresIn = 3600 // seconds
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

        /// <summary>
        /// Register endpoint - creates new user and returns JWT token
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already registered" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Address = request.Address ?? string.Empty,
                EmailConfirmed = false // Require email confirmation
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                // Assign Customer role
                await _userManager.AddToRoleAsync(user, "Customer");

                _logger.LogInformation("New user registered via API: {Email}", user.Email);

                // Note: In production, send email confirmation here
                // For now, auto-confirm for API users
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);

                var jwtToken = await _jwtTokenService.GenerateTokenAsync(user);

                return Ok(new
                {
                    message = "Registration successful",
                    token = jwtToken,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles = new[] { "Customer" },
                    expiresIn = 3600
                });
            }

            return BadRequest(new { message = "Registration failed", errors = result.Errors.Select(e => e.Description) });
        }
    }

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
}