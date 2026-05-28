using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    internal class CustomersViewModel : BaseViewModel
    {
        private readonly CustomerService _customerService;
        private ObservableCollection<dynamic> _customers = new();
        private ObservableCollection<dynamic> _allCustomers = new();  // cache voor search
        private dynamic? _selectedCustomer;
        private string _searchQuery = string.Empty;
        private RelayCommand? _loadCustomersCommand;
        private RelayCommand? _createCustomerCommand;
        private RelayCommand? _editCustomerCommand;
        private RelayCommand? _deleteCustomerCommand;
        private RelayCommand? _searchCommand;
        private RelayCommand? _refreshCommand;

        // ── Dialog events ────────────────────────────────────────────────────
        public event EventHandler? CreateCustomerRequested;
        public event EventHandler<dynamic>? EditCustomerRequested;

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

        // ── Load ──────────────────────────────────────────────────────────────
        private async void ExecuteLoadCustomers()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var customers = await _customerService.GetAllCustomersAsync();
                _allCustomers = new ObservableCollection<dynamic>(customers ?? new List<dynamic>());
                Customers = new ObservableCollection<dynamic>(_allCustomers);
                if (!Customers.Any())
                    ErrorMessage = "No customers found.";
            }
            catch (Exception ex) { ErrorMessage = $"Error loading customers: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Create / Edit (raise events; view opens dialog) ───────────────────
        private void ExecuteCreateCustomer() =>
            CreateCustomerRequested?.Invoke(this, EventArgs.Empty);

        private void ExecuteEditCustomer()
        {
            if (SelectedCustomer == null) { ErrorMessage = "No customer selected."; return; }
            EditCustomerRequested?.Invoke(this, SelectedCustomer);
        }

        private bool CanExecuteEditCustomer() => SelectedCustomer != null && !IsBusy;

        // ── Delete ────────────────────────────────────────────────────────────
        private async void ExecuteDeleteCustomer()
        {
            if (SelectedCustomer == null) { ErrorMessage = "No customer selected."; return; }
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
                else { ErrorMessage = "Failed to delete customer."; }
            }
            catch (Exception ex) { ErrorMessage = $"Error deleting customer: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private bool CanExecuteDeleteCustomer() => SelectedCustomer != null && !IsBusy;

        // ── Search (client-side) ──────────────────────────────────────────────
        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                Customers = new ObservableCollection<dynamic>(_allCustomers);
                return;
            }
            var q = SearchQuery.Trim().ToLowerInvariant();
            // LINQ query-syntax: client-side zoeken op naam en e-mail
            var results = (from c in _allCustomers
                           where (c.FirstName?.ToString() ?? "").ToLower().Contains(q) ||
                                 (c.LastName?.ToString() ?? "").ToLower().Contains(q) ||
                                 (c.Email?.ToString() ?? "").ToLower().Contains(q)
                           select c).ToList();
            Customers = new ObservableCollection<dynamic>(results);
        }

        // ── Submit handlers (called by view after dialog OK) ──────────────────

        /// <summary>
        /// Assumes CustomerService.CreateCustomerAsync(string, string, string, string, string, bool) exists.
        /// </summary>
        public async Task SubmitCreateCustomerAsync(
            string firstName, string lastName,
            string email, string phone,
            string address, bool isActive)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var success = await _customerService.CreateCustomerAsync(
                    firstName, lastName, email, phone, address, isActive);
                if (success) { SuccessMessage = "Klant aangemaakt."; ExecuteLoadCustomers(); }
                else ErrorMessage = "Aanmaken mislukt.";
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij aanmaken: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Assumes CustomerService.UpdateCustomerAsync(string, string, string, string, string, string, bool) exists.
        /// </summary>
        public async Task SubmitUpdateCustomerAsync(
            string customerId,
            string firstName, string lastName,
            string email, string phone,
            string address, bool isActive)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var success = await _customerService.UpdateCustomerAsync(
                    customerId, firstName, lastName, email, phone, address, isActive);
                if (success) { SuccessMessage = "Klant bijgewerkt."; ExecuteLoadCustomers(); }
                else ErrorMessage = "Bijwerken mislukt.";
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij bijwerken: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}