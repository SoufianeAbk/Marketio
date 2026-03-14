using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.ComponentModel.DataAnnotations;

namespace Marketio_App.ViewModels
{
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly AuthService _authService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        [Required(ErrorMessage = "Email is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig emailadres")]
        private string email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [MinLength(6, ErrorMessage = "Wachtwoord moet minimaal 6 tekens lang zijn")]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool rememberMe;

        public LoginViewModel(AuthService authService, ConnectivityService connectivity)
        {
            _authService = authService;
            _connectivity = connectivity;
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email en wachtwoord zijn verplicht.";
                return;
            }

            if (!_connectivity.IsConnected)
            {
                ErrorMessage = "Geen internetverbinding beschikbaar.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var success = await _authService.LoginAsync(Email, Password);

                if (success)
                {
                    if (RememberMe)
                    {
                        await SecureStorage.Default.SetAsync("remember_email", Email);
                    }

                    await Shell.Current.GoToAsync("///producten");
                }
                else
                {
                    ErrorMessage = "Inloggen mislukt. Controleer uw gegevens.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Een fout is opgetreden: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("register");
        }

        [RelayCommand]
        public async Task LoadRememberedEmailAsync()
        {
            try
            {
                var savedEmail = await SecureStorage.Default.GetAsync("remember_email");
                if (!string.IsNullOrWhiteSpace(savedEmail))
                {
                    Email = savedEmail;
                    RememberMe = true;
                }
            }
            catch
            {
                // Silently ignore if secure storage fails
            }
        }
    }
}