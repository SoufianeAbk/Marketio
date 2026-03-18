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
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly ProductApiService _productService;
        private readonly ConnectivityService _connectivity;

        [ObservableProperty]
        private ObservableCollection<ProductDto> products = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private ProductCategory? selectedCategory;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        private IEnumerable<ProductDto> _allProducts = new List<ProductDto>();

        public ProductsViewModel(ProductApiService productService, ConnectivityService connectivity)
        {
            _productService = productService;
            _connectivity = connectivity;
        }

        [RelayCommand]
        public async Task LoadProductsAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var products = await _productService.GetAllProductsAsync();
                _allProducts = products ?? new List<ProductDto>();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden producten: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RefreshProductsAsync()
        {
            try
            {
                IsRefreshing = true;
                ErrorMessage = string.Empty;

                var products = await _productService.GetAllProductsAsync();
                _allProducts = products ?? new List<ProductDto>();

                ApplyFilters();
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
        public async Task SelectProductAsync(ProductDto product)
        {
            if (product == null)
                return;

            await Shell.Current.GoToAsync($"product-detail?productId={product.Id}");
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedCategoryChanged(ProductCategory? value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allProducts.AsEnumerable();

            // Filter op categorie (null betekent "geen filter")
            if (SelectedCategory.HasValue)
            {
                filtered = filtered.Where(p => p.Category == SelectedCategory.Value);
            }

            // Filter op zoekterm
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(query) ||
                    p.Description.ToLower().Contains(query));
            }

            Products = new ObservableCollection<ProductDto>(filtered);
        }
    }
}