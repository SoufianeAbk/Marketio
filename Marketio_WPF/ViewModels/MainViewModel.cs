using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using Marketio_WPF.Services.Interfaces;
using Marketio_WPF.Views;
using System.Windows.Controls;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application.
    /// Manages overall application state and navigation.
    /// </summary>
    internal class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly ProductService _productService;
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private readonly UserManagementService _userManagementService;

        private object? _currentView;
        private string _currentUserName = string.Empty;
        private bool _isAuthenticated;
        private bool _isAdmin;
        private bool _isEmployee;

        private RelayCommand? _logoutCommand;
        private RelayCommand? _navigateToOrdersCommand;
        private RelayCommand? _navigateToProductsCommand;
        private RelayCommand? _navigateToCustomersCommand;
        private RelayCommand? _navigateToAdminCommand;
        private RelayCommand? _navigateToRegisterCommand;

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        /// <summary>Zichtbaarheid van Admin- en Gebruiker-registratie-knoppen</summary>
        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        /// <summary>Zichtbaarheid van Klanten-knop (Medewerker of Admin)</summary>
        public bool IsEmployee
        {
            get => _isEmployee;
            set => SetProperty(ref _isEmployee, value);
        }

        public RelayCommand LogoutCommand => _logoutCommand ??= new RelayCommand(ExecuteLogout);
        public RelayCommand NavigateToProductsCommand => _navigateToProductsCommand ??= new RelayCommand(ExecuteNavigateToProducts);
        public RelayCommand NavigateToOrdersCommand => _navigateToOrdersCommand ??= new RelayCommand(ExecuteNavigateToOrders);
        public RelayCommand NavigateToCustomersCommand => _navigateToCustomersCommand ??= new RelayCommand(ExecuteNavigateToCustomers);
        public RelayCommand NavigateToAdminCommand => _navigateToAdminCommand ??= new RelayCommand(ExecuteNavigateToAdmin);
        public RelayCommand NavigateToRegisterCommand => _navigateToRegisterCommand ??= new RelayCommand(ExecuteNavigateToRegister);

        public MainViewModel(
            IAuthService authService,
            ProductService productService,
            OrderService orderService,
            CustomerService customerService,
            UserManagementService userManagementService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));

            LoadUserInfo();
            LoadDefaultView();
        }

        private void LoadUserInfo()
        {
            try
            {
                IsAuthenticated = _authService.IsAuthenticated;
                CurrentUserName = _authService.CurrentUser?.UserName ?? "Guest";

                // Rollen asynchroon laden
                if (_authService.CurrentUser != null)
                {
                    _ = LoadUserRolesAsync(_authService.CurrentUser.Id);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading user information: {ex.Message}";
                CurrentUserName = "Guest";
            }
        }

        private async Task LoadUserRolesAsync(string userId)
        {
            try
            {
                var roles = await _authService.GetUserRolesAsync(userId);
                IsAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                IsEmployee = roles.Contains("Employee", StringComparer.OrdinalIgnoreCase) || IsAdmin;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading user roles: {ex.Message}";
            }
        }

        private void LoadDefaultView()
        {
            ExecuteNavigateToProducts();
        }

        private void ExecuteNavigateToProducts()
        {
            try
            {
                ClearMessages();
                var viewModel = new ProductsViewModel(_productService);
                CurrentView = new ProductsView { DataContext = viewModel };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error navigating to products: {ex.Message}";
            }
        }

        private void ExecuteNavigateToOrders()
        {
            try
            {
                ClearMessages();
                var viewModel = new OrdersViewModel(_orderService);
                CurrentView = new OrdersView { DataContext = viewModel };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error navigating to orders: {ex.Message}";
            }
        }

        private void ExecuteNavigateToCustomers()
        {
            try
            {
                ClearMessages();
                var viewModel = new CustomersViewModel(_customerService);
                CurrentView = new CustomersView { DataContext = viewModel };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error navigating to customers: {ex.Message}";
            }
        }

        private void ExecuteNavigateToAdmin()
        {
            try
            {
                ClearMessages();
                var viewModel = new AdminViewModel(_userManagementService);
                CurrentView = new AdminView { DataContext = viewModel };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error navigating to admin: {ex.Message}";
            }
        }

        private void ExecuteNavigateToRegister()
        {
            try
            {
                ClearMessages();
                var viewModel = new RegisterViewModel(_authService);
                CurrentView = new RegisterView { DataContext = viewModel };
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error navigating to register: {ex.Message}";
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
                CurrentUserName = "Guest";
                SuccessMessage = "Logged out successfully.";

                // Reload default view
                LoadDefaultView();
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
    }
}