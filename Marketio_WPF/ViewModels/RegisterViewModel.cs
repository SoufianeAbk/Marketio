using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using System.Net.Http;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for user registration.
    /// Handles form validation and user account creation.
    /// </summary>
    internal class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private string _email = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _acceptTerms;
        private bool _acceptPrivacyPolicy;
        private RelayCommand? _registerCommand;
        private RelayCommand? _backToLoginCommand;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public bool AcceptTerms
        {
            get => _acceptTerms;
            set => SetProperty(ref _acceptTerms, value);
        }

        public bool AcceptPrivacyPolicy
        {
            get => _acceptPrivacyPolicy;
            set => SetProperty(ref _acceptPrivacyPolicy, value);
        }

        public RelayCommand RegisterCommand => _registerCommand ??= new RelayCommand(ExecuteRegister, CanExecuteRegister);
        public RelayCommand BackToLoginCommand => _backToLoginCommand ??= new RelayCommand(ExecuteBackToLogin);

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private async void ExecuteRegister()
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _authService.RegisterAsync(Email, FirstName, LastName, Password);

                if (success)
                {
                    SuccessMessage = "Registration successful. Please log in.";
                    ClearForm();
                }
                else
                {
                    ErrorMessage = "Registration failed. Please try again.";
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

        private bool CanExecuteRegister()
        {
            return !IsBusy && ValidateForm();
        }

        private void ExecuteBackToLogin()
        {
            // Navigation would be handled by view or main window
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "All required fields must be filled.";
                return false;
            }

            if (!Email.Contains("@") || Email.Length < 5 || Email.Length > 254)
            {
                ErrorMessage = "Invalid email address.";
                return false;
            }

            if (FirstName.Length > 100)
            {
                ErrorMessage = "First name cannot exceed 100 characters.";
                return false;
            }

            if (LastName.Length > 100)
            {
                ErrorMessage = "Last name cannot exceed 100 characters.";
                return false;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return false;
            }

            if (!AcceptTerms || !AcceptPrivacyPolicy)
            {
                ErrorMessage = "You must accept the terms and privacy policy.";
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            AcceptTerms = false;
            AcceptPrivacyPolicy = false;
        }
    }
}