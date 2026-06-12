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
        private readonly LocalDatabaseService _localDatabase;
        private readonly SecureKeyManagementService _keyManagement;

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
            ConnectivityService connectivity,
            LocalDatabaseService localDatabase,
            SecureKeyManagementService keyManagement)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
            _localDatabase = localDatabase ?? throw new ArgumentNullException(nameof(localDatabase));
            _keyManagement = keyManagement ?? throw new ArgumentNullException(nameof(keyManagement));

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

                    // Laad audit trail
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
                    // Sla op op het apparaat
                    var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    await File.WriteAllBytesAsync(filePath, fileContent);

                    SuccessMessage = $"Gegevens geëxporteerd naar: {fileName}";

                    // Deel het bestand indien gewenst
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

                    // ─── AVG-naleving: Verwijder alle lokale gegevens ───
                    await ClearAllLocalDataAsync();

                    // Uitloggen na succesvolle verwijderingsaanvraag
                    await Task.Delay(2000);
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

        /// <summary>
        /// Wist alle gecachede gegevens en vernieuwt het profiel.
        /// Aanroepen bij het inloggen met een nieuwe gebruiker.
        /// </summary>
        [RelayCommand]
        public async Task RefreshProfileDataAsync()
        {
            try
            {
                // Wis alle gecachede eigenschappen
                Email = string.Empty;
                FirstName = string.Empty;
                LastName = string.Empty;
                Address = null;
                PhoneNumber = null;
                MarketingOptIn = false;
                IsDeletionRequested = false;
                AuditLogs.Clear();
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                // Herlaad profiel met verse gegevens
                await LoadProfileAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij vernieuwen: {ex.Message}";
            }
        }

        // AVG-naleving: Privé-hulpmethoden

        /// <summary>
        /// Wist alle lokale gegevens conform het AVG-recht op vergetelheid.
        /// Omvat de database, versleutelingssleutels en tijdelijke bestanden.
        /// AVG Artikel 17 – Recht op wissing.
        /// </summary>
        private async Task ClearAllLocalDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] AVG-gegevensopruiming gestart...");

                // Stap 1: Wis alle lokale database-inhoud
                await _localDatabase.ClearAllDataAsync();
                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] ✓ Lokale database gewist.");

                // Stap 2: Verwijder versleutelingssleutel uit beveiligde opslag
                await _keyManagement.ClearDatabaseKeyAsync();
                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] ✓ Versleutelingssleutel verwijderd.");

                // Stap 3: Wis authenticatietoken
                await _authService.LogoutAsync();
                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] ✓ Authenticatietoken gewist.");

                // Stap 4: Wis tijdelijke cachebestanden
                await ClearCacheFilesAsync();
                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] ✓ Cachebestanden gewist.");

                System.Diagnostics.Debug.WriteLine("[AccountSettingsViewModel] AVG-gegevensopruiming succesvol afgerond.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AccountSettingsViewModel] Waarschuwing: fout tijdens AVG-opruiming: {ex.Message}");
                // Loggen maar niet gooien – verwijderingsaanvraag is al geslaagd op de server
            }
        }

        /// <summary>
        /// Wist tijdelijke cachebestanden (geëxporteerde gegevens, tijdelijke downloads, enz.).
        /// Zoekt naar bestanden die overeenkomen met het patroon "marketio-*".
        /// </summary>
        private async Task ClearCacheFilesAsync()
        {
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                if (Directory.Exists(cacheDir))
                {
                    var files = Directory.GetFiles(cacheDir, "marketio-*");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            System.Diagnostics.Debug.WriteLine($"[AccountSettingsViewModel] Cachebestand verwijderd: {file}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[AccountSettingsViewModel] Verwijderen van cachebestand mislukt {file}: {ex.Message}");
                        }
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AccountSettingsViewModel] Fout bij wissen van cache: {ex.Message}");
            }
        }

        // Privé-hulpmethode (geen relay command)

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