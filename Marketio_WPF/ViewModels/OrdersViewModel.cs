using CommunityToolkit.Mvvm.Input;
using Marketio_Shared.Enums;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    internal class OrdersViewModel : BaseViewModel
    {
        // ── Services ──────────────────────────────────────────────────────────
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;

        // ── Backing fields ────────────────────────────────────────────────────
        private ObservableCollection<dynamic> _orders = new();
        private ObservableCollection<dynamic> _allOrders = new();   // cache voor client-side filter
        private dynamic? _selectedOrder;
        private string _statusFilter = "All";

        private RelayCommand? _loadOrdersCommand;
        private RelayCommand? _createOrderCommand;
        private RelayCommand? _updateOrderCommand;
        private RelayCommand? _deleteOrderCommand;
        private RelayCommand? _filterByStatusCommand;
        private RelayCommand? _refreshCommand;

        // ── Dialog events ─────────────────────────────────────────────────────

        /// <summary>Raised wanneer de view de CreateOrderDialog moet openen.</summary>
        public event EventHandler<(List<dynamic> Customers, List<dynamic> Products)>? CreateOrderRequested;

        /// <summary>Raised wanneer de view de OrderStatusDialog moet openen.</summary>
        public event EventHandler<dynamic>? UpdateOrderRequested;

        // ── Properties ────────────────────────────────────────────────────────
        public ObservableCollection<dynamic> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public dynamic? SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public RelayCommand LoadOrdersCommand => _loadOrdersCommand ??= new RelayCommand(ExecuteLoadOrders);
        public RelayCommand CreateOrderCommand => _createOrderCommand ??= new RelayCommand(ExecuteCreateOrder, CanExecuteCreateOrder);
        public RelayCommand UpdateOrderCommand => _updateOrderCommand ??= new RelayCommand(ExecuteUpdateOrder, CanExecuteUpdateOrder);
        public RelayCommand DeleteOrderCommand => _deleteOrderCommand ??= new RelayCommand(ExecuteDeleteOrder, CanExecuteDeleteOrder);
        public RelayCommand FilterByStatusCommand => _filterByStatusCommand ??= new RelayCommand(ExecuteFilterByStatus);
        public RelayCommand RefreshCommand => _refreshCommand ??= new RelayCommand(ExecuteLoadOrders);

        // ── Constructor ───────────────────────────────────────────────────────
        public OrdersViewModel(
            OrderService orderService,
            CustomerService customerService,
            ProductService productService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private async void ExecuteLoadOrders()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var orders = await _orderService.GetAllOrdersAsync();
                _allOrders = new ObservableCollection<dynamic>(orders ?? new List<dynamic>());
                Orders = new ObservableCollection<dynamic>(_allOrders);
                if (!Orders.Any())
                    ErrorMessage = "Geen orders gevonden.";
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij laden van orders: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Create (laadt klanten + producten, gooit event naar view) ─────────
        private async void ExecuteCreateOrder()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var customers = await _customerService.GetAllCustomersAsync();
                var products = await _productService.GetAllProductsAsync();
                CreateOrderRequested?.Invoke(this, (customers, products));
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij laden gegevens: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private bool CanExecuteCreateOrder() => !IsBusy;

        /// <summary>
        /// Wordt aangeroepen door OrdersView nadat de dialog OK is bevestigd.
        /// Slaat de nieuwe order op en herlaadt de lijst.
        /// </summary>
        public async Task SubmitCreateOrderAsync(
            string customerId,
            string shippingAddress,
            string billingAddress,
            PaymentMethod paymentMethod,
            List<(int ProductId, int Quantity, decimal UnitPrice)> items)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var success = await _orderService.CreateOrderAsync(
                    customerId, shippingAddress, billingAddress, paymentMethod, items);

                if (success)
                {
                    SuccessMessage = "Order succesvol aangemaakt.";
                    ExecuteLoadOrders();
                }
                else
                {
                    ErrorMessage = "Order aanmaken mislukt.";
                }
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij aanmaken order: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Update status (raises event; view opens dialog) ───────────────────
        private void ExecuteUpdateOrder()
        {
            if (SelectedOrder == null) { ErrorMessage = "Geen order geselecteerd."; return; }
            UpdateOrderRequested?.Invoke(this, SelectedOrder);
        }

        private bool CanExecuteUpdateOrder() => SelectedOrder != null && !IsBusy;

        // ── Delete ────────────────────────────────────────────────────────────
        private async void ExecuteDeleteOrder()
        {
            if (SelectedOrder == null) { ErrorMessage = "Geen order geselecteerd."; return; }
            try
            {
                IsBusy = true;
                ClearMessages();
                var orderId = (int)SelectedOrder.Id;
                var success = await _orderService.DeleteOrderAsync(orderId);
                if (success)
                {
                    Orders.Remove(SelectedOrder);
                    SuccessMessage = "Order verwijderd.";
                    SelectedOrder = null;
                }
                else { ErrorMessage = "Verwijderen mislukt."; }
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij verwijderen: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private bool CanExecuteDeleteOrder() => SelectedOrder != null && !IsBusy;

        // ── Filter (client-side) ──────────────────────────────────────────────
        private void ExecuteFilterByStatus()
        {
            if (string.IsNullOrEmpty(StatusFilter) || StatusFilter == "All")
            {
                Orders = new ObservableCollection<dynamic>(_allOrders);
                return;
            }
            // LINQ query-syntax: client-side filter op status
            var filtered = (from o in _allOrders
                            where o.StatusName?.ToString() == StatusFilter
                            select o).ToList();
            Orders = new ObservableCollection<dynamic>(filtered);
        }

        // ── Submit handler voor status-update (aangeroepen door view) ─────────

        /// <summary>
        /// Aangeroepen door OrdersView na bevestiging van de OrderStatusDialog.
        /// Converteert de statusnaam string naar het OrderStatus enum.
        /// </summary>
        public async Task SubmitStatusUpdateAsync(int orderId, string statusName)
        {
            if (!Enum.TryParse<OrderStatus>(statusName, ignoreCase: true, out var status))
            {
                ErrorMessage = $"Onbekende status: '{statusName}'.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();
                var success = await _orderService.UpdateOrderStatusAsync(orderId, status);
                if (success)
                {
                    SuccessMessage = "Orderstatus bijgewerkt.";
                    ExecuteLoadOrders();
                }
                else { ErrorMessage = "Status bijwerken mislukt."; }
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij bijwerken: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
