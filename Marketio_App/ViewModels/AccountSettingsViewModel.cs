using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace Marketio_App.ViewModels
{
    public partial class AccountSettingsViewModel : ObservableObject
    {
        private readonly AccountApiService _accountService;
        private readonly AuthService _authService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string? address;

        [ObservableProperty]
        private string? phoneNumber;

        [ObservableProperty]
        private bool marketingOptIn;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private bool isDeletionRequested;

        [ObservableProperty]
        private ObservableCollection<AccountApiService.AuditLogDto> auditLogs = new();

        [ObservableProperty]
        private bool showDeletionDialog;

        [ObservableProperty]
        private string deletionPassword = string.Empty;

        [ObservableProperty]
        private bool isOffline;

        public AccountSettingsViewModel(
            AccountApiService accountService,
            AuthService authService,
            ConnectivityService connectivity)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));

            isOffline = !_connectivity.IsConnected;
            _connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object? sender, bool isConnected)
        {
            IsOffline = !isConnected;
        }

        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var (success, profile, error) = await _accountService.GetProfileAsync();

                if (success && profile != null)
                {
                    Email = profile.Email;
                    FirstName = profile.FirstName;
                    LastName = profile.LastName;
                    Address = profile.Address;
                    PhoneNumber = profile.PhoneNumber;
                    MarketingOptIn = profile.MarketingOptIn;
                    IsDeletionRequested = profile.IsDeletionRequested;

                    // Load audit trail
                    await LoadAuditTrailInternalAsync();
                }
                else
                {
                    ErrorMessage = error ?? "Kon profiel niet laden.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task UpdateConsentAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var (success, error) = await _accountService.UpdateConsentAsync(MarketingOptIn);

                if (success)
                {
                    SuccessMessage = MarketingOptIn
                        ? "Marketing toestemming ingeschakeld."
                        : "Marketing toestemming ingetrokken.";
                }
                else
                {
                    ErrorMessage = error ?? "Kon toestemming niet bijwerken.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ExportDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var (success, fileContent, fileName, error) = await _accountService.ExportDataAsync();

                if (success && fileContent != null && fileName != null)
                {
                    // Save to device
                    var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    await File.WriteAllBytesAsync(filePath, fileContent);

                    SuccessMessage = $"Gegevens geëxporteerd naar: {fileName}";

                    // Optionally share the file
                    if (File.Exists(filePath))
                    {
                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Mijn persoonlijke gegevens",
                            File = new ShareFile(filePath)
                        });
                    }
                }
                else
                {
                    ErrorMessage = error ?? "Kon gegevens niet exporteren.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij exporteren: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void ShowDeletionConfirmation()
        {
            DeletionPassword = string.Empty;
            ShowDeletionDialog = true;
        }

        [RelayCommand]
        public void CancelDeletion()
        {
            ShowDeletionDialog = false;
            DeletionPassword = string.Empty;
        }

        [RelayCommand]
        public async Task ConfirmDeletionAsync()
        {
            if (string.IsNullOrWhiteSpace(DeletionPassword))
            {
                ErrorMessage = "Vul uw wachtwoord in.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var (success, error) = await _accountService.RequestDeletionAsync(DeletionPassword);

                if (success)
                {
                    SuccessMessage = "Verwijderingsaanvraag ingediend. Uw account wordt binnen 30 dagen verwijderd.";
                    ShowDeletionDialog = false;
                    DeletionPassword = string.Empty;
                    IsDeletionRequested = true;

                    // Log out after successful deletion request
                    await Task.Delay(2000);
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync("///login");
                }
                else
                {
                    ErrorMessage = error ?? "Kon verwijderingsaanvraag niet verwerken.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            try
            {
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij uitloggen: {ex.Message}";
            }
        }

        // ─── Private helper method (not a relay command) ───────────────────────

        private async Task LoadAuditTrailInternalAsync()
        {
            try
            {
                var (success, logs, error) = await _accountService.GetAuditTrailAsync();

                if (success && logs != null)
                {
                    AuditLogs = new ObservableCollection<AccountApiService.AuditLogDto>(logs);
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    ErrorMessage = error;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden auditgegevens: {ex.Message}";
            }
        }
    }
}