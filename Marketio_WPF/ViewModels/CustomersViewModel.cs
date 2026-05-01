using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for managing customers.
    /// Handles customer listing, search, and CRUD operations.
    /// </summary>
    internal class CustomersViewModel : BaseViewModel
    {
        private readonly CustomerService _customerService;
        private ObservableCollection<dynamic> _customers = new();
        private dynamic? _selectedCustomer;
        private string _searchQuery = string.Empty;
        private RelayCommand? _loadCustomersCommand;
        private RelayCommand? _createCustomerCommand;
        private RelayCommand? _editCustomerCommand;
        private RelayCommand? _deleteCustomerCommand;
        private RelayCommand? _searchCommand;
        private RelayCommand? _refreshCommand;

        public ObservableCollection<dynamic> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public dynamic? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public RelayCommand LoadCustomersCommand => _loadCustomersCommand ??= new RelayCommand(ExecuteLoadCustomers);
        public RelayCommand CreateCustomerCommand => _createCustomerCommand ??= new RelayCommand(ExecuteCreateCustomer);
        public RelayCommand EditCustomerCommand => _editCustomerCommand ??= new RelayCommand(ExecuteEditCustomer, CanExecuteEditCustomer);
        public RelayCommand DeleteCustomerCommand => _deleteCustomerCommand ??= new RelayCommand(ExecuteDeleteCustomer, CanExecuteDeleteCustomer);
        public RelayCommand SearchCommand => _searchCommand ??= new RelayCommand(ExecuteSearch);
        public RelayCommand RefreshCommand => _refreshCommand ??= new RelayCommand(ExecuteLoadCustomers);

        public CustomersViewModel(CustomerService customerService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        }

        private async void ExecuteLoadCustomers()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var customers = await _customerService.GetAllCustomersAsync();
                Customers = new ObservableCollection<dynamic>(customers ?? new List<dynamic>());

                if (!Customers.Any())
                {
                    ErrorMessage = "No customers found.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading customers: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteCreateCustomer()
        {
            // Navigation and dialog would be handled by view
        }

        private void ExecuteEditCustomer()
        {
            if (SelectedCustomer == null)
            {
                ErrorMessage = "No customer selected.";
                return;
            }

            // Navigation and dialog would be handled by view
        }

        private bool CanExecuteEditCustomer()
        {
            return SelectedCustomer != null && !IsBusy;
        }

        private async void ExecuteDeleteCustomer()
        {
            if (SelectedCustomer == null)
            {
                ErrorMessage = "No customer selected.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var customerId = (string)SelectedCustomer.Id;
                var success = await _customerService.DeleteCustomerAsync(customerId);

                if (success)
                {
                    Customers.Remove(SelectedCustomer);
                    SuccessMessage = "Customer deleted successfully.";
                    SelectedCustomer = null;
                }
                else
                {
                    ErrorMessage = "Failed to delete customer.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting customer: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteDeleteCustomer()
        {
            return SelectedCustomer != null && !IsBusy;
        }

        private void ExecuteSearch()
        {
            // Search logic would be implemented here
            // Could call a search API endpoint or filter locally
        }
    }
}