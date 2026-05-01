using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application.
    /// Manages overall application state and navigation.
    /// </summary>
    internal class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private RelayCommand? _logoutCommand;
        private RelayCommand? _navigateToOrdersCommand;
        private RelayCommand? _navigateToProductsCommand;
        private RelayCommand? _navigateToCustomersCommand;
        private RelayCommand? _navigateToAdminCommand;
        private string _currentUser = string.Empty;
        private bool _isAuthenticated;

        public string CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        public RelayCommand LogoutCommand => _logoutCommand ??= new RelayCommand(ExecuteLogout);
        public RelayCommand NavigateToOrdersCommand => _navigateToOrdersCommand ??= new RelayCommand(() => OnNavigate("Orders"));
        public RelayCommand NavigateToProductsCommand => _navigateToProductsCommand ??= new RelayCommand(() => OnNavigate("Products"));
        public RelayCommand NavigateToCustomersCommand => _navigateToCustomersCommand ??= new RelayCommand(() => OnNavigate("Customers"));
        public RelayCommand NavigateToAdminCommand => _navigateToAdminCommand ??= new RelayCommand(() => OnNavigate("Admin"));

        public event EventHandler<string>? NavigationRequested;

        public MainViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            try
            {
                IsAuthenticated = _authService.IsAuthenticated;
                CurrentUser = _authService.CurrentUser?.UserName ?? "Guest";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading user information: {ex.Message}";
            }
        }

        private async void ExecuteLogout()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                await _authService.LogoutAsync();
                IsAuthenticated = false;
                CurrentUser = "Guest";
                SuccessMessage = "Logged out successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Logout error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnNavigate(string view)
        {
            NavigationRequested?.Invoke(this, view);
        }
    }
}