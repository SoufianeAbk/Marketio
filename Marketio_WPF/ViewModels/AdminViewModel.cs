using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Models;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel voor administratieve functies.
    /// Beheert gebruikersbeheer, roltoewijzingen en systeembrede bewerkingen.
    /// </summary>
    internal class AdminViewModel : BaseViewModel
    {
        private readonly UserManagementService _userManagementService;

        // Gebruikers
        private ObservableCollection<UserAdminDto> _users = new();
        private UserAdminDto? _selectedUser;

        public ObservableCollection<UserAdminDto> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public UserAdminDto? SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                AssignRoleCommand.NotifyCanExecuteChanged();
                RemoveRoleCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
                LockUserCommand.NotifyCanExecuteChanged();
                UnlockUserCommand.NotifyCanExecuteChanged();
            }
        }

        // Rollen
        private ObservableCollection<string> _availableRoles = new();
        private string _selectedRole = string.Empty;

        public ObservableCollection<string> AvailableRoles
        {
            get => _availableRoles;
            set => SetProperty(ref _availableRoles, value);
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                SetProperty(ref _selectedRole, value);
                AssignRoleCommand.NotifyCanExecuteChanged();
                RemoveRoleCommand.NotifyCanExecuteChanged();
            }
        }

        // Commands
        public RelayCommand LoadUsersCommand { get; }
        public RelayCommand LoadRolesCommand { get; }
        public RelayCommand AssignRoleCommand { get; }
        public RelayCommand RemoveRoleCommand { get; }
        public RelayCommand DeleteUserCommand { get; }
        public RelayCommand LockUserCommand { get; }
        public RelayCommand UnlockUserCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // Constructor
        public AdminViewModel(UserManagementService userManagementService)
        {
            _userManagementService = userManagementService
                ?? throw new ArgumentNullException(nameof(userManagementService));

            LoadUsersCommand = new RelayCommand(ExecuteLoadUsers);
            LoadRolesCommand = new RelayCommand(ExecuteLoadRoles);
            AssignRoleCommand = new RelayCommand(ExecuteAssignRole, CanExecuteAssignRole);
            RemoveRoleCommand = new RelayCommand(ExecuteRemoveRole, CanExecuteRemoveRole);
            DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteDeleteUser);
            LockUserCommand = new RelayCommand(ExecuteLockUser, CanExecuteLockUser);
            UnlockUserCommand = new RelayCommand(ExecuteUnlockUser, CanExecuteUnlockUser);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
        }

        // Sequentiële initialisatie (gebruikt door UserControl_Loaded en na mutaties)
        /// <summary>
        /// Laadt rollen en gebruikers sequentieel om concurrente DbContext-toegang
        /// te vermijden. Altijd via deze methode initialiseren, niet via de losse
        /// Load-commands tegelijk aanroepen.
        /// </summary>
        public async Task InitializeAsync()
        {
            ClearMessages();
            IsBusy = true;

            try
            {
                // 1. Rollen eerst — eenvoudige query, geen loop
                try
                {
                    var roles = await _userManagementService.GetAllRolesAsync();
                    AvailableRoles = new ObservableCollection<string>(
                        roles.Where(r => !string.IsNullOrWhiteSpace(r)));
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Fout bij laden van rollen: {ex.Message}";
                }

                // 2. Gebruikers daarna — bevat meerdere awaits per user
                try
                {
                    var users = await _userManagementService.GetAllUsersAsync();
                    Users = new ObservableCollection<UserAdminDto>(users);

                    if (!Users.Any())
                        ErrorMessage = "Geen gebruikers gevonden.";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Fout bij laden van gebruikers: {ex.Message}";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // 1. Gebruikers Laden (intern, niet meer rechtstreeks aangeroepen bij initialisatie)
        private async void ExecuteLoadUsers()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var users = await _userManagementService.GetAllUsersAsync();
                Users = new ObservableCollection<UserAdminDto>(users);

                if (!Users.Any())
                    ErrorMessage = "Geen gebruikers gevonden.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden van gebruikers: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // 2. Beschikbare Rollen Laden (intern, niet meer rechtstreeks aangeroepen bij initialisatie)
        private async void ExecuteLoadRoles()
        {
            try
            {
                var roles = await _userManagementService.GetAllRolesAsync();
                AvailableRoles = new ObservableCollection<string>(
                    roles.Where(r => !string.IsNullOrWhiteSpace(r)));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden van rollen: {ex.Message}";
            }
        }

        // 3. Rol Toekennen
        // Na de mutatie wordt InitializeAsync() gebruikt (ipv de losse ExecuteLoadUsers),
        // zodat rollen en gebruikers sequentieel worden herladen zonder concurrent DbContext-gebruik.
        // SuccessMessage wordt NA de refresh ingesteld zodat InitializeAsync().ClearMessages()
        // de boodschap niet meteen wist.
        private async void ExecuteAssignRole()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var roleName = SelectedRole;
                var success = await _userManagementService.AssignRoleAsync(
                    SelectedUser!.Id, roleName);

                if (success)
                {
                    await InitializeAsync();                                   // sequentieel herladen
                    SuccessMessage = $"Rol '{roleName}' succesvol toegewezen."; // NA de refresh
                }
                else
                {
                    ErrorMessage = "Rol toewijzen mislukt. " +
                                   "De rol bestaat mogelijk niet of de gebruiker heeft de rol al.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij toewijzen van rol: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteAssignRole() =>
            SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedRole) && !IsBusy;

        // 4. Verwijder Rol
        private async void ExecuteRemoveRole()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var roleName = SelectedRole;
                var success = await _userManagementService.RemoveRoleAsync(
                    SelectedUser!.Id, roleName);

                if (success)
                {
                    await InitializeAsync();                                    // sequentieel herladen
                    SuccessMessage = $"Rol '{roleName}' succesvol verwijderd."; // NA de refresh
                }
                else
                {
                    ErrorMessage = "Rol verwijderen mislukt. " +
                                   "Mogelijk heeft de gebruiker deze rol niet.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij verwijderen van rol: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteRemoveRole() =>
            SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedRole) && !IsBusy;

        // 5. Delete user (GDPR Right to be Forgotten)
        private async void ExecuteDeleteUser()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _userManagementService.DeleteUserAsync(SelectedUser!.Id);

                if (success)
                {
                    Users.Remove(SelectedUser);
                    SuccessMessage = "Gebruiker permanent verwijderd (AVG-recht op vergetelheid).";
                    SelectedUser = null;
                }
                else
                {
                    ErrorMessage = "Gebruiker verwijderen mislukt.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij verwijderen van gebruiker: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteDeleteUser() => SelectedUser != null && !IsBusy;

        // 6. Blokkeer gebruiker (permanent — DateTimeOffset.MaxValue)
        // CanExecute: alleen actief als de geselecteerde gebruiker NIET al geblokkeerd is.
        // FullName wordt voor InitializeAsync() vastgelegd zodat de SuccessMessage na
        // het herladen nog de juiste naam toont.
        private async void ExecuteLockUser()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = SelectedUser!.Id;
                var fullName = SelectedUser.FullName;
                var success = await _userManagementService.LockUserAsync(userId);

                if (success)
                {
                    await InitializeAsync();                                          // sequentieel herladen
                    SuccessMessage = $"Gebruiker '{fullName}' succesvol geblokkeerd."; // NA de refresh
                }
                else
                {
                    ErrorMessage = "Blokkeren mislukt.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij blokkeren van gebruiker: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteLockUser() =>
            SelectedUser != null && !SelectedUser.IsLocked && !IsBusy;

        // 7. Ontgrendel gebruiker
        // CanExecute: alleen actief als de geselecteerde gebruiker WEL vergrendeld is.
        private async void ExecuteUnlockUser()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = SelectedUser!.Id;
                var fullName = SelectedUser.FullName;
                var success = await _userManagementService.UnlockUserAsync(userId);

                if (success)
                {
                    await InitializeAsync();                                      // sequentieel herladen
                    SuccessMessage = $"Gebruiker '{fullName}' succesvol gedeblokkeerd."; // NA de refresh
                }
                else
                {
                    ErrorMessage = "Ontgrendelen mislukt.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij ontgrendelen van gebruiker: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteUnlockUser() =>
            SelectedUser != null && SelectedUser.IsLocked && !IsBusy;

        // 8. Refresh
        private async void ExecuteRefresh()
        {
            await InitializeAsync();
        }
    }
}