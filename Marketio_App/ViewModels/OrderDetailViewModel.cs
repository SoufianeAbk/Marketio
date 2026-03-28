using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
using Microsoft.Maui.Controls;

namespace Marketio_App.ViewModels
{
    [QueryProperty(nameof(OrderId), nameof(OrderId))]
    public partial class OrderDetailViewModel : ObservableObject
    {
        private readonly OrderApiService _orderService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        private OrderDto? order;

        [ObservableProperty]
        private int orderId;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasOrder;

        public OrderDetailViewModel(OrderApiService orderService, ConnectivityService connectivity)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        }

        partial void OnOrderIdChanged(int value)
        {
            if (value > 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadOrderAsync(value);
                });
            }
        }

        [RelayCommand]
        public async Task LoadOrderAsync(int id)
        {
            if (!_connectivity.IsConnected)
            {
                ErrorMessage = "Geen internetverbinding beschikbaar.";
                HasOrder = false;
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                Order = await _orderService.GetOrderByIdAsync(id);

                if (Order == null)
                {
                    ErrorMessage = "Bestelling niet gevonden.";
                    HasOrder = false;
                }
                else
                {
                    HasOrder = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden bestelling: {ex.Message}";
                HasOrder = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RefreshOrderAsync()
        {
            if (OrderId > 0)
            {
                await LoadOrderAsync(OrderId);
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("///orders");
        }
    }
}