using CommunityToolkit.Mvvm.Input;
using Marketio_Shared.Enums;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    internal class OrdersViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private ObservableCollection<dynamic> _orders = new();
        private ObservableCollection<dynamic> _allOrders = new();   // cache voor client-side filter
        private dynamic? _selectedOrder;
        private string _statusFilter = "All";
        private RelayCommand? _loadOrdersCommand;
        private RelayCommand? _updateOrderCommand;
        private RelayCommand? _deleteOrderCommand;
        private RelayCommand? _filterByStatusCommand;
        private RelayCommand? _refreshCommand;

        // ── Dialog event ─────────────────────────────────────────────────────
        public event EventHandler<dynamic>? UpdateOrderRequested;

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

        public RelayCommand LoadOrdersCommand => _loadOrdersCommand ??= new RelayCommand(ExecuteLoadOrders);
        public RelayCommand UpdateOrderCommand => _updateOrderCommand ??= new RelayCommand(ExecuteUpdateOrder, CanExecuteUpdateOrder);
        public RelayCommand DeleteOrderCommand => _deleteOrderCommand ??= new RelayCommand(ExecuteDeleteOrder, CanExecuteDeleteOrder);
        public RelayCommand FilterByStatusCommand => _filterByStatusCommand ??= new RelayCommand(ExecuteFilterByStatus);
        public RelayCommand RefreshCommand => _refreshCommand ??= new RelayCommand(ExecuteLoadOrders);

        public OrdersViewModel(OrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
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
                    ErrorMessage = "No orders found.";
            }
            catch (Exception ex) { ErrorMessage = $"Error loading orders: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Update status (raises event; view opens dialog) ───────────────────
        private void ExecuteUpdateOrder()
        {
            if (SelectedOrder == null) { ErrorMessage = "No order selected."; return; }
            UpdateOrderRequested?.Invoke(this, SelectedOrder);
        }

        private bool CanExecuteUpdateOrder() => SelectedOrder != null && !IsBusy;

        // ── Delete ────────────────────────────────────────────────────────────
        private async void ExecuteDeleteOrder()
        {
            if (SelectedOrder == null) { ErrorMessage = "No order selected."; return; }
            try
            {
                IsBusy = true;
                ClearMessages();
                var orderId = (int)SelectedOrder.Id;
                var success = await _orderService.DeleteOrderAsync(orderId);
                if (success)
                {
                    Orders.Remove(SelectedOrder);
                    SuccessMessage = "Order deleted successfully.";
                    SelectedOrder = null;
                }
                else { ErrorMessage = "Failed to delete order."; }
            }
            catch (Exception ex) { ErrorMessage = $"Error deleting order: {ex.Message}"; }
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

        // ── Submit handler (called by view after dialog OK) ───────────────────

        /// <summary>
        /// Called by OrdersView after the status dialog is confirmed.
        /// FIX CS1503: statusName string wordt geparsed naar het OrderStatus enum
        /// voordat het doorgegeven wordt aan OrderService.UpdateOrderStatusAsync.
        /// </summary>
        public async Task SubmitStatusUpdateAsync(int orderId, string statusName)
        {
            // FIX: converteer string naar OrderStatus enum
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