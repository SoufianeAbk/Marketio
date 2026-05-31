using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Models;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for administrative functions.
    /// Handles user management, role assignment, and system-wide operations.
    /// </summary>
    internal class AdminViewModel : BaseViewModel
    {
        private readonly UserManagementService _userManagementService;

        // ── Users ────────────────────────────────────────────────────────────
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

                // Re-evaluate all user-specific commands when selection changes.
                AssignRoleCommand.NotifyCanExecuteChanged();
                RemoveRoleCommand.NotifyCanExecuteChanged();
                ResetPasswordCommand.NotifyCanExecuteChanged();
                LockUserCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
            }
        }

        // ── Roles ────────────────────────────────────────────────────────────
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

        // ── Commands ─────────────────────────────────────────────────────────
        public RelayCommand LoadUsersCommand { get; }
        public RelayCommand LoadRolesCommand { get; }
        public RelayCommand AssignRoleCommand { get; }
        public RelayCommand RemoveRoleCommand { get; }
        public RelayCommand ResetPasswordCommand { get; }
        public RelayCommand LockUserCommand { get; }
        public RelayCommand DeleteUserCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public AdminViewModel(UserManagementService userManagementService)
        {
            _userManagementService = userManagementService
                ?? throw new ArgumentNullException(nameof(userManagementService));

            LoadUsersCommand = new RelayCommand(ExecuteLoadUsers);
            LoadRolesCommand = new RelayCommand(ExecuteLoadRoles);
            AssignRoleCommand = new RelayCommand(ExecuteAssignRole, CanExecuteAssignRole);
            RemoveRoleCommand = new RelayCommand(ExecuteRemoveRole, CanExecuteRemoveRole);
            ResetPasswordCommand = new RelayCommand(ExecuteResetPassword, CanExecuteResetPassword);
            LockUserCommand = new RelayCommand(ExecuteLockUser, CanExecuteLockUser);
            DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteDeleteUser);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
        }

        // ── 1. Load users ─────────────────────────────────────────────────────
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

        // ── 2. Load available roles ───────────────────────────────────────────
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

        // ── 3. Assign role ────────────────────────────────────────────────────
        private async void ExecuteAssignRole()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _userManagementService.AssignRoleAsync(
                    SelectedUser!.Id, SelectedRole);

                if (success)
                {
                    SuccessMessage = $"Rol '{SelectedRole}' succesvol toegewezen.";
                    ExecuteLoadUsers();
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

        // ── 4. Remove role ────────────────────────────────────────────────────
        private async void ExecuteRemoveRole()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _userManagementService.RemoveRoleAsync(
                    SelectedUser!.Id, SelectedRole);

                if (success)
                {
                    SuccessMessage = $"Rol '{SelectedRole}' succesvol verwijderd.";
                    ExecuteLoadUsers();
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

        // ── 5. Reset password ─────────────────────────────────────────────────
        private async void ExecuteResetPassword()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _userManagementService.ResetPasswordAsync(SelectedUser!.Id);

                if (success)
                    SuccessMessage = "Wachtwoord-reset token aangemaakt. " +
                                     "Stuur de reset-link naar de gebruiker.";
                else
                    ErrorMessage = "Wachtwoord resetten mislukt.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij wachtwoord resetten: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteResetPassword() => SelectedUser != null && !IsBusy;

        // ── 6. Lock user ──────────────────────────────────────────────────────
        private async void ExecuteLockUser()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var success = await _userManagementService.LockUserAsync(SelectedUser!.Id);

                if (success)
                {
                    SuccessMessage = "Account geblokkeerd.";
                    ExecuteLoadUsers();
                }
                else
                {
                    ErrorMessage = "Account blokkeren mislukt.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij blokkeren van account: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteLockUser() => SelectedUser != null && !IsBusy;

        // ── 7. Delete user (GDPR Right to be Forgotten) ───────────────────────
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

        // ── 8. Refresh ────────────────────────────────────────────────────────
        private void ExecuteRefresh()
        {
            ClearMessages();
            ExecuteLoadUsers();
            ExecuteLoadRoles();
        }
    }
}
