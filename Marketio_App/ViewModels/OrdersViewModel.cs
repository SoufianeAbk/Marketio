using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
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
        private bool isOffline;

        [ObservableProperty]
        private int pendingOrderCount;

        [ObservableProperty]
        private bool isSyncing;

        [ObservableProperty]
        private string syncMessage = string.Empty;

        public OrdersViewModel(OrderApiService orderService, ConnectivityService connectivity)
        {
            _orderService = orderService;
            _connectivity = connectivity;

            isOffline = !_connectivity.IsConnected;

            _connectivity.ConnectivityChanged += OnConnectivityChanged;

            // Toon melding na automatische sync door OrderApiService
            _orderService.PendingOrdersSynced += OnPendingOrdersSynced;
        }

        // ─── Connectiviteit ───────────────────────────────────────────────────────

        private async void OnConnectivityChanged(object? sender, bool isConnected)
        {
            IsOffline = !isConnected;

            if (isConnected)
            {
                // OrderApiService triggert zelf de sync; wij wachten en herladen daarna
                await Task.Delay(500); // kleine vertraging zodat sync kan starten
                await RefreshOrdersCommand.ExecuteAsync(null);
            }
        }

        private async void OnPendingOrdersSynced(object? sender, int count)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                SyncMessage = $"{count} offline bestelling(en) succesvol gesynchroniseerd.";
                await RefreshOrdersCommand.ExecuteAsync(null);

                // Verberg melding na 4 seconden
                await Task.Delay(4000);
                SyncMessage = string.Empty;
            });
        }

        // ─── Laden / vernieuwen ───────────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadOrdersAsync()
        {
            if (IsLoading)
                return;

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

                PendingOrderCount = await _orderService.GetPendingOrderCountAsync();
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

                PendingOrderCount = await _orderService.GetPendingOrderCountAsync();
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

        // ─── Handmatige sync ─────────────────────────────────────────────────────

        [RelayCommand]
        public async Task ManualSyncAsync()
        {
            if (!_connectivity.IsConnected)
            {
                ErrorMessage = "Geen internetverbinding beschikbaar.";
                return;
            }

            try
            {
                IsSyncing = true;
                ErrorMessage = string.Empty;

                var synced = await _orderService.SyncPendingOrdersAsync();

                if (synced > 0)
                {
                    SyncMessage = $"{synced} bestelling(en) gesynchroniseerd.";
                    await RefreshOrdersAsync();
                    await Task.Delay(4000);
                    SyncMessage = string.Empty;
                }
                else
                {
                    SyncMessage = "Geen bestellingen in de wachtrij.";
                    await Task.Delay(2000);
                    SyncMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Sync mislukt: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        // ─── Navigatie / selectie ─────────────────────────────────────────────────

        [RelayCommand]
        public async Task SelectOrderAsync(OrderDto? order)
        {
            if (order == null)
                return;

            if (order.Id == 0)
            {
                // Pending order: navigatie naar detail heeft geen zin
                await Application.Current!.MainPage!.DisplayAlert(
                    "In wachtrij",
                    $"Bestelling {order.OrderNumber} staat in de offline wachtrij en wordt verstuurd zodra u verbinding heeft.",
                    "OK");
                return;
            }

            await Shell.Current.GoToAsync($"order-detail?OrderId={order.Id}");
        }

        [RelayCommand]
        public async Task DeleteOrderAsync(OrderDto? order)
        {
            if (order == null)
                return;

            if (order.Id == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Wachtrij-bestelling",
                    "Offline wachtrij-bestellingen kunnen momenteel niet worden verwijderd.",
                    "OK");
                return;
            }

            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Bestelling verwijderen",
                $"Weet u zeker dat u bestelling {order.OrderNumber} wilt verwijderen?",
                "Ja", "Nee");

            if (!confirm)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                bool deleted = await _orderService.DeleteOrderAsync(order.Id);

                if (deleted)
                {
                    Orders.Remove(order);
                    HasOrders = Orders.Any();
                    await Application.Current.MainPage!.DisplayAlert("Succes", "Bestelling is verwijderd.", "OK");
                }
                else
                {
                    ErrorMessage = "Bestelling kon niet worden verwijderd.";
                    await Application.Current.MainPage!.DisplayAlert("Fout", ErrorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij verwijderen: {ex.Message}";
                await Application.Current.MainPage!.DisplayAlert("Fout", ErrorMessage, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CreateNewOrderAsync()
        {
            await Shell.Current.GoToAsync("create-order");
        }
    }
}