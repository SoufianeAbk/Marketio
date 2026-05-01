using CommunityToolkit.Mvvm.Input;
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
        private ObservableCollection<dynamic> _users = new();
        private dynamic? _selectedUser;
        private string _selectedRole = string.Empty;
        private RelayCommand? _loadUsersCommand;
        private RelayCommand? _assignRoleCommand;
        private RelayCommand? _removeRoleCommand;
        private RelayCommand? _resetPasswordCommand;
        private RelayCommand? _lockUserCommand;
        private RelayCommand? _deleteUserCommand;
        private RelayCommand? _refreshCommand;

        public ObservableCollection<dynamic> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public dynamic? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public RelayCommand LoadUsersCommand => _loadUsersCommand ??= new RelayCommand(ExecuteLoadUsers);
        public RelayCommand AssignRoleCommand => _assignRoleCommand ??= new RelayCommand(ExecuteAssignRole, CanExecuteAssignRole);
        public RelayCommand RemoveRoleCommand => _removeRoleCommand ??= new RelayCommand(ExecuteRemoveRole, CanExecuteRemoveRole);
        public RelayCommand ResetPasswordCommand => _resetPasswordCommand ??= new RelayCommand(ExecuteResetPassword, CanExecuteResetPassword);
        public RelayCommand LockUserCommand => _lockUserCommand ??= new RelayCommand(ExecuteLockUser, CanExecuteLockUser);
        public RelayCommand DeleteUserCommand => _deleteUserCommand ??= new RelayCommand(ExecuteDeleteUser, CanExecuteDeleteUser);
        public RelayCommand RefreshCommand => _refreshCommand ??= new RelayCommand(ExecuteLoadUsers);

        public AdminViewModel(UserManagementService userManagementService)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        private async void ExecuteLoadUsers()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var users = await _userManagementService.GetAllUsersAsync();
                Users = new ObservableCollection<dynamic>(users ?? new List<dynamic>());

                if (!Users.Any())
                {
                    ErrorMessage = "No users found.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading users: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ExecuteAssignRole()
        {
            if (SelectedUser == null || string.IsNullOrWhiteSpace(SelectedRole))
            {
                ErrorMessage = "Please select a user and role.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = (string)SelectedUser.Id;
                var success = await _userManagementService.AssignRoleAsync(userId, SelectedRole);

                if (success)
                {
                    SuccessMessage = $"Role '{SelectedRole}' assigned successfully.";
                }
                else
                {
                    ErrorMessage = "Failed to assign role.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error assigning role: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteAssignRole()
        {
            return SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedRole) && !IsBusy;
        }

        private async void ExecuteRemoveRole()
        {
            if (SelectedUser == null || string.IsNullOrWhiteSpace(SelectedRole))
            {
                ErrorMessage = "Please select a user and role.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = (string)SelectedUser.Id;
                var success = await _userManagementService.RemoveRoleAsync(userId, SelectedRole);

                if (success)
                {
                    SuccessMessage = $"Role '{SelectedRole}' removed successfully.";
                }
                else
                {
                    ErrorMessage = "Failed to remove role.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error removing role: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteRemoveRole()
        {
            return SelectedUser != null && !string.IsNullOrWhiteSpace(SelectedRole) && !IsBusy;
        }

        private async void ExecuteResetPassword()
        {
            if (SelectedUser == null)
            {
                ErrorMessage = "Please select a user.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = (string)SelectedUser.Id;
                var success = await _userManagementService.ResetPasswordAsync(userId);

                if (success)
                {
                    SuccessMessage = "Password reset email sent to user.";
                }
                else
                {
                    ErrorMessage = "Failed to reset password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error resetting password: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteResetPassword()
        {
            return SelectedUser != null && !IsBusy;
        }

        private async void ExecuteLockUser()
        {
            if (SelectedUser == null)
            {
                ErrorMessage = "Please select a user.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = (string)SelectedUser.Id;
                var success = await _userManagementService.LockUserAsync(userId);

                if (success)
                {
                    SuccessMessage = "User account locked.";
                }
                else
                {
                    ErrorMessage = "Failed to lock user account.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error locking user: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteLockUser()
        {
            return SelectedUser != null && !IsBusy;
        }

        private async void ExecuteDeleteUser()
        {
            if (SelectedUser == null)
            {
                ErrorMessage = "Please select a user.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var userId = (string)SelectedUser.Id;
                var success = await _userManagementService.DeleteUserAsync(userId);

                if (success)
                {
                    Users.Remove(SelectedUser);
                    SuccessMessage = "User deleted successfully.";
                    SelectedUser = null;
                }
                else
                {
                    ErrorMessage = "Failed to delete user.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting user: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteDeleteUser()
        {
            return SelectedUser != null && !IsBusy;
        }
    }
}