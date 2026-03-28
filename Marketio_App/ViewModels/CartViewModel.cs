using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace Marketio_App.ViewModels
{
    public partial class CartViewModel : ObservableObject
    {
        private readonly CartService _cartService;

        [ObservableProperty]
        private ObservableCollection<CartItemDto> cartItems = new();

        [ObservableProperty]
        private decimal cartTotal;

        [ObservableProperty]
        private bool isCartEmpty;

        public CartViewModel(CartService cartService)
        {
            _cartService = cartService;
        }

        [RelayCommand]
        public async Task LoadCartAsync()
        {
            try
            {
                var items = await _cartService.GetCartItemsAsync();
                CartItems = new ObservableCollection<CartItemDto>(items);
                CartTotal = await _cartService.GetCartTotalAsync();
                IsCartEmpty = !items.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij laden winkelwagen: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task RemoveItemAsync(int productId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(productId);
                await LoadCartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij verwijderen item: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task IncreaseQuantityAsync(int productId)
        {
            try
            {
                var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
                if (item != null && item.Quantity < item.AvailableStock)
                {
                    await _cartService.UpdateCartItemAsync(productId, item.Quantity + 1);
                    await LoadCartAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij verhogen hoeveelheid: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DecreaseQuantityAsync(int productId)
        {
            try
            {
                var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
                if (item != null && item.Quantity > 1)
                {
                    await _cartService.UpdateCartItemAsync(productId, item.Quantity - 1);
                    await LoadCartAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij verlagen hoeveelheid: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task GoToCheckoutAsync()
        {
            if (CartItems.Any())
            {
                await Shell.Current.GoToAsync("create-order");
            }
        }

        [RelayCommand]
        public async Task GoToProductsAsync()
        {
            await Shell.Current.GoToAsync("///producten");
        }
    }
}