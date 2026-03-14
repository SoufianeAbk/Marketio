using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace Marketio_App.ViewModels
{
    public partial class OrdersViewModel : ObservableObject
    {
        private readonly OrderApiService _orderService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        private ObservableCollection<OrderDto> orders = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasOrders;

        [ObservableProperty]
        private OrderStatus? selectedStatusFilter;

        public OrdersViewModel(OrderApiService orderService, ConnectivityService connectivity)
        {
            _orderService = orderService;
            _connectivity = connectivity;
        }

        [RelayCommand]
        public async Task LoadOrdersAsync()
        {
            if (IsLoading)
                return;

            if (!_connectivity.IsConnected)
            {
                ErrorMessage = "Geen internetverbinding beschikbaar.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var myOrders = await _orderService.GetMyOrdersAsync();

                if (myOrders != null)
                {
                    var orderList = myOrders.ToList();
                    Orders = new ObservableCollection<OrderDto>(orderList);
                    HasOrders = orderList.Any();
                }
                else
                {
                    ErrorMessage = "Kan bestellingen niet ophalen.";
                    HasOrders = false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden bestellingen: {ex.Message}";
                HasOrders = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RefreshOrdersAsync()
        {
            try
            {
                IsRefreshing = true;
                ErrorMessage = string.Empty;

                var myOrders = await _orderService.GetMyOrdersAsync();

                if (myOrders != null)
                {
                    var orderList = myOrders.ToList();
                    Orders = new ObservableCollection<OrderDto>(orderList);
                    HasOrders = orderList.Any();
                }
                else
                {
                    ErrorMessage = "Kan bestellingen niet ophalen.";
                    HasOrders = false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Vernieuwen mislukt: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task SelectOrderAsync(OrderDto order)
        {
            if (order == null)
                return;

            await Shell.Current.GoToAsync($"order-detail?orderId={order.Id}");
        }

        [RelayCommand]
        public async Task CreateNewOrderAsync()
        {
            await Shell.Current.GoToAsync("create-order");
        }

        partial void OnSelectedStatusFilterChanged(OrderStatus? value)
        {
            // Filter orders by status if needed
        }
    }
}