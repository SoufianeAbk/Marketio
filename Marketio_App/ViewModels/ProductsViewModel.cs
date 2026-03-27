using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marketio_App.Services;
using Marketio_Shared.DTOs;
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
        private ObservableCollection<ProductCategory> categories = new();

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
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));

            InitializeCategories();
        }

        private void InitializeCategories()
        {
            var categoryValues = Enum.GetValues<ProductCategory>();
            categories = new ObservableCollection<ProductCategory>(categoryValues);
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

                System.Diagnostics.Debug.WriteLine("[ProductsViewModel] LoadProductsAsync started");

                var products = await _productService.GetAllProductsAsync();
                var productList = products?.ToList() ?? new List<ProductDto>();

                System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] Received {productList.Count} products from service");

                // ✅ Update the all products list
                _allProducts = productList;

                // ✅ Update the UI collection
                ApplyFilters();

                System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] LoadProductsAsync completed: {Products.Count} items in UI");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fout bij laden producten: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] LoadProductsAsync error: {ex}");
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

                System.Diagnostics.Debug.WriteLine("[ProductsViewModel] RefreshProductsAsync started");

                var products = await _productService.GetAllProductsAsync();
                var productList = products?.ToList() ?? new List<ProductDto>();

                _allProducts = productList;
                ApplyFilters();

                System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] RefreshProductsAsync completed: {Products.Count} items");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Vernieuwen mislukt: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] RefreshProductsAsync error: {ex}");
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

            await Shell.Current.GoToAsync($"///product-detail?productId={product.Id}");
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

            if (SelectedCategory.HasValue)
            {
                filtered = filtered.Where(p => p.Category == SelectedCategory.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(query) ||
                    p.Description.ToLower().Contains(query));
            }

            var filteredList = filtered.ToList();
            Products = new ObservableCollection<ProductDto>(filteredList);

            System.Diagnostics.Debug.WriteLine($"[ProductsViewModel] ApplyFilters: {Products.Count} items after filtering");
        }
    }
}