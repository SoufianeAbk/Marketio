using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for managing orders.
    /// Handles order listing, filtering, status updates, and deletion.
    /// </summary>
    internal class OrdersViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private ObservableCollection<dynamic> _orders = new();
        private dynamic? _selectedOrder;
        private string _statusFilter = "All";
        private RelayCommand? _loadOrdersCommand;
        private RelayCommand? _updateOrderCommand;
        private RelayCommand? _deleteOrderCommand;
        private RelayCommand? _filterByStatusCommand;
        private RelayCommand? _refreshCommand;

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

        private async void ExecuteLoadOrders()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var orders = await _orderService.GetAllOrdersAsync();
                Orders = new ObservableCollection<dynamic>(orders ?? new List<dynamic>());

                if (!Orders.Any())
                {
                    ErrorMessage = "No orders found.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading orders: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ExecuteUpdateOrder()
        {
            if (SelectedOrder == null)
            {
                ErrorMessage = "No order selected.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                // Dialog or form interaction would be handled by view
                SuccessMessage = "Order updated successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating order: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteUpdateOrder()
        {
            return SelectedOrder != null && !IsBusy;
        }

        private async void ExecuteDeleteOrder()
        {
            if (SelectedOrder == null)
            {
                ErrorMessage = "No order selected.";
                return;
            }

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
                else
                {
                    ErrorMessage = "Failed to delete order.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting order: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteDeleteOrder()
        {
            return SelectedOrder != null && !IsBusy;
        }

        private void ExecuteFilterByStatus()
        {
            // Filtering logic would be implemented here
            // Could call a filtered API endpoint or filter locally
        }
    }
}