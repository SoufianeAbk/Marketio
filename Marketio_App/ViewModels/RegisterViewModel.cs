using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Microsoft.Maui.Controls;
using System.ComponentModel.DataAnnotations;

namespace Marketio_App.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool acceptTerms;

        public RegisterViewModel(AuthService authService, ConnectivityService connectivity)
        {
            _authService = authService;
            _connectivity = connectivity;
        }

        [RelayCommand]
        public async Task RegisterAsync()
        {
            if (!ValidateForm())
            {
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

                var success = await _authService.RegisterAsync(
                    Email,
                    FirstName,
                    LastName,
                    string.IsNullOrWhiteSpace(Address) ? null : Address,
                    Password);

                if (success)
                {
                    await Shell.Current.GoToAsync("///producten");
                }
                else
                {
                    ErrorMessage = "Registratie mislukt. Probeer het later opnieuw.";
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
        public async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("../login");
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vul alle verplichte velden in.";
                return false;
            }

            if (!Email.Contains("@") || Email.Length < 5)
            {
                ErrorMessage = "Ongeldig emailadres";
                return false;
            }

            if (Email.Length > 254)
            {
                ErrorMessage = "Email is te lang.";
                return false;
            }

            if (FirstName.Length > 100)
            {
                ErrorMessage = "Voornaam mag maximaal 100 tekens zijn";
                return false;
            }

            if (LastName.Length > 100)
            {
                ErrorMessage = "Achternaam mag maximaal 100 tekens zijn";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Address) && Address.Length > 500)
            {
                ErrorMessage = "Adres mag maximaal 500 tekens zijn";
                return false;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Wachtwoord moet minimaal 6 tekens lang zijn";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Wachtwoorden komen niet overeen.";
                return false;
            }

            if (!AcceptTerms)
            {
                ErrorMessage = "U moet de voorwaarden accepteren.";
                return false;
            }

            return true;
        }
    }
}