using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
using Marketio_Shared.Entities;
using Microsoft.Maui.Controls;

namespace Marketio_App.ViewModels
{
    [QueryProperty(nameof(ProductId), nameof(ProductId))]
    public partial class ProductDetailViewModel : ObservableObject
    {
        private readonly ProductApiService _productService;
        private readonly CartService _cartService;

        [ObservableProperty]
        private ProductDto? product;

        [ObservableProperty]
        private int productId;

        [ObservableProperty]
        private int quantity = 1;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private decimal totalPrice;

        public ProductDetailViewModel(ProductApiService productService, CartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
        }

        partial void OnProductIdChanged(int value)
        {
            if (value > 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadProductAsync(value);
                });
            }
        }

        partial void OnQuantityChanged(int value)
        {
            UpdateTotalPrice();
        }

        [RelayCommand]
        public async Task LoadProductAsync(int id)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                Product = await _productService.GetProductByIdAsync(id);

                if (Product == null)
                {
                    ErrorMessage = "Product niet gevonden.";
                }
                else
                {
                    UpdateTotalPrice();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden product: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void IncreaseQuantity()
        {
            if (Product != null && Quantity < Product.Stock)
            {
                Quantity++;
            }
        }

        [RelayCommand]
        public void DecreaseQuantity()
        {
            if (Quantity > 1)
            {
                Quantity--;
            }
        }

        [RelayCommand]
        public async Task AddToCartAsync()
        {
            if (Product == null)
            {
                ErrorMessage = "Product niet beschikbaar.";
                return;
            }

            try
            {
                await _cartService.AddToCartAsync(Product, Quantity);
                await Shell.Current.DisplayAlert(
                    "Succes",
                    $"{Product.Name} ({Quantity}x) toegevoegd aan winkelwagen",
                    "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij toevoegen aan winkelwagen: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void UpdateTotalPrice()
        {
            if (Product != null)
            {
                TotalPrice = Product.Price * Quantity;
            }
        }
    }
}