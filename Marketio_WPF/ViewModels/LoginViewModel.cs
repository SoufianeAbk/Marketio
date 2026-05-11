using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services.Interfaces;
using Marketio_WPF.Services;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for user login functionality.
    /// Handles authentication and credential validation.
    /// </summary>
    internal class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginViewModel>? _logger;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private RelayCommand? _loginCommand;
        private RelayCommand? _registerCommand;

        /// <summary>
        /// Event raised when login is successful and navigation should occur.
        /// </summary>
        public event EventHandler? LoginSucceeded;

        /// <summary>
        /// Event raised when user requests to navigate to register page.
        /// </summary>
        public event EventHandler? RegisterRequested;

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    _loginCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    _loginCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public RelayCommand LoginCommand => _loginCommand ??= new RelayCommand(ExecuteLoginAsync, CanExecuteLogin);
        public RelayCommand RegisterCommand => _registerCommand ??= new RelayCommand(ExecuteRegister);

        public LoginViewModel(IAuthService authService, ILogger<LoginViewModel>? logger = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger;
        }

        private async void ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email and password are required.";
                _logger?.LogWarning("Login attempt with empty email or password");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                _logger?.LogInformation("Login attempt for email: {Email}", Email);

                var success = await _authService.LoginAsync(Email, Password);

                if (success)
                {
                    SuccessMessage = "Login successful.";
                    _logger?.LogInformation("Login successful for email: {Email}", Email);
                    OnLoginSucceeded();
                }
                else
                {
                    ErrorMessage = "Invalid email or password. Please try again. Test users: admin@marketio.be / Admin@12345 or user@marketio.be / User@12345";
                    _logger?.LogWarning("Login failed for email: {Email} - Invalid credentials", Email);
                }
            }
            catch (HttpRequestException httpEx)
            {
                ErrorMessage = "Unable to connect to the server. Please check if the API is active.";
                _logger?.LogError(httpEx, "HTTP error during login for email: {Email}", Email);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                _logger?.LogError(ex, "Unexpected error during login for email: {Email}", Email);
            }
            finally
            {
                IsBusy = false;
                _loginCommand?.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteLogin()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteRegister()
        {
            OnRegisterRequested();
        }

        protected virtual void OnLoginSucceeded()
        {
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRegisterRequested()
        {
            RegisterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}