using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using System.Net.Http;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for user login functionality.
    /// Handles authentication and credential validation.
    /// </summary>
    internal class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
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
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public RelayCommand LoginCommand => _loginCommand ??= new RelayCommand(ExecuteLogin, CanExecuteLogin);
        public RelayCommand RegisterCommand => _registerCommand ??= new RelayCommand(ExecuteRegister);

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private async void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email and password are required.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _authService.LoginAsync(Email, Password);

                if (success)
                {
                    SuccessMessage = "Login successful.";
                    // Raise event to notify the view to navigate to MainWindow
                    OnLoginSucceeded();
                }
                else
                {
                    ErrorMessage = "Invalid email or password. Please try again.";
                }
            }
            catch (HttpRequestException)
            {
                ErrorMessage = "Unable to connect to the server. Please check if the API is active.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
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