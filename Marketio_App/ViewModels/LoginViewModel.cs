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
        private readonly ApiService _apiService;
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

        public LoginViewModel(AuthService authService, ApiService apiService, ConnectivityService connectivity)
        {
            _authService = authService;
            _apiService = apiService;
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

                var (success, errorMessage) = await _authService.LoginAsync(Email, Password);

                if (success)
                {
                    // Get the newly saved token and update ApiService
                    var token = await _authService.GetTokenAsync();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        _apiService.SetAuthorizationHeader(token);
                    }

                    if (RememberMe)
                    {
                        await SecureStorage.Default.SetAsync("remember_email", Email);
                    }

                    await Shell.Current.GoToAsync("///producten");
                }
                else
                {
                    ErrorMessage = errorMessage ?? "Inloggen mislukt. Controleer uw e-mailadres en wachtwoord.";
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                ErrorMessage = "Kan geen verbinding maken met de server. Controleer of de API actief is.";
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
            await Shell.Current.GoToAsync("///register");
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