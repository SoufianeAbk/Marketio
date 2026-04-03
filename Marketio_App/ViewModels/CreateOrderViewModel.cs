using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace Marketio_App.ViewModels
{
    public partial class CreateOrderViewModel : ObservableObject
    {
        private readonly CartService _cartService;
        private readonly OrderApiService _orderService;
        private readonly ConnectivityService _connectivity;
        private readonly AuthService _authService;

        [ObservableProperty]
        private ObservableCollection<CartItemDto> cartItems = new();

        [ObservableProperty]
        private decimal cartTotal;

        [ObservableProperty]
        private string shippingAddress = string.Empty;

        [ObservableProperty]
        private string billingAddress = string.Empty;

        [ObservableProperty]
        private bool sameAsShipping = true;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isCartEmpty;

        public CreateOrderViewModel(
            CartService cartService,
            OrderApiService orderService,
            ConnectivityService connectivity,
            AuthService authService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _connectivity = connectivity;
            _authService = authService;
        }

        [RelayCommand]
        public async Task LoadCartAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var items = await _cartService.GetCartItemsAsync();
                CartItems = new ObservableCollection<CartItemDto>(items);
                CartTotal = await _cartService.GetCartTotalAsync();
                IsCartEmpty = !items.Any();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden winkelwagen: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RemoveItemAsync(int productId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(productId);
                var items = await _cartService.GetCartItemsAsync();
                CartItems = new ObservableCollection<CartItemDto>(items);
                CartTotal = await _cartService.GetCartTotalAsync();
                IsCartEmpty = !items.Any();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij verwijderen item: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task PlaceOrderAsync()
        {
            if (!CartItems.Any())
            {
                ErrorMessage = "Winkelwagen is leeg.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ShippingAddress))
            {
                ErrorMessage = "Verzendadres is verplicht.";
                return;
            }

            if (string.IsNullOrWhiteSpace(BillingAddress) && !SameAsShipping)
            {
                ErrorMessage = "Factuuradres is verplicht.";
                return;
            }

            if (!_connectivity.IsConnected)
            {
                ErrorMessage = "Geen internetverbinding beschikbaar.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var createOrderDto = new CreateOrderDto
                {
                    ShippingAddress = ShippingAddress,
                    BillingAddress = SameAsShipping ? ShippingAddress : BillingAddress,
                    OrderItems = CartItems.Select(item => new CreateOrderItemDto
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                var order = await _orderService.CreateOrderAsync(createOrderDto);

                if (order != null && order.Id > 0)
                {
                    await _cartService.ClearCartAsync();
                    await Shell.Current.DisplayAlert(
                        "Succes",
                        $"Bestelling {order.OrderNumber} succesvol geplaatst!",
                        "OK");
                    await Shell.Current.GoToAsync($"order-detail?OrderId={order.Id}");
                }
                else
                {
                    ErrorMessage = "Kon bestelling niet plaatsen. Probeer later opnieuw.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Token expired or invalid - redirect to login
                ErrorMessage = "Sessie verlopen. U wordt teruggeleid naar inloggen.";
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij plaatsen bestelling: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("cart");
        }

        partial void OnSameAsShippingChanged(bool value)
        {
            if (value)
            {
                BillingAddress = ShippingAddress;
            }
        }

        partial void OnShippingAddressChanged(string value)
        {
            if (SameAsShipping)
            {
                BillingAddress = value;
            }
        }
    }
}