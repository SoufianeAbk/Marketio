using CommunityToolkit.Mvvm.Input;
using Marketio_Shared.DTOs;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    internal class ProductsViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private ObservableCollection<dynamic> _products = new();
        private dynamic? _selectedProduct;
        private string _searchQuery = string.Empty;
        private RelayCommand? _loadProductsCommand;
        private RelayCommand? _createProductCommand;
        private RelayCommand? _editProductCommand;
        private RelayCommand? _deleteProductCommand;
        private RelayCommand? _refreshCommand;

        // ── Dialog events ────────────────────────────────────────────────────
        public event EventHandler? CreateProductRequested;
        public event EventHandler<dynamic>? EditProductRequested;

        public ObservableCollection<dynamic> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public dynamic? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public RelayCommand LoadProductsCommand => _loadProductsCommand ??= new RelayCommand(ExecuteLoadProducts);
        public RelayCommand CreateProductCommand => _createProductCommand ??= new RelayCommand(ExecuteCreateProduct);
        public RelayCommand EditProductCommand => _editProductCommand ??= new RelayCommand(ExecuteEditProduct, CanExecuteEditProduct);
        public RelayCommand DeleteProductCommand => _deleteProductCommand ??= new RelayCommand(ExecuteDeleteProduct, CanExecuteDeleteProduct);
        public RelayCommand RefreshCommand => _refreshCommand ??= new RelayCommand(ExecuteLoadProducts);

        public ProductsViewModel(ProductService productService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        private async void ExecuteLoadProducts()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var products = await _productService.GetAllProductsAsync();
                Products = new ObservableCollection<dynamic>(products ?? new List<dynamic>());
                if (!Products.Any())
                    ErrorMessage = "No products found.";
            }
            catch (Exception ex) { ErrorMessage = $"Error loading products: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Create / Edit (raise events; view opens dialog) ───────────────────
        private void ExecuteCreateProduct() =>
            CreateProductRequested?.Invoke(this, EventArgs.Empty);

        private void ExecuteEditProduct()
        {
            if (SelectedProduct == null) { ErrorMessage = "No product selected."; return; }
            EditProductRequested?.Invoke(this, SelectedProduct);
        }

        private bool CanExecuteEditProduct() => SelectedProduct != null && !IsBusy;

        // ── Delete ────────────────────────────────────────────────────────────
        private async void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null) { ErrorMessage = "No product selected."; return; }
            try
            {
                IsBusy = true;
                ClearMessages();
                var productId = (int)SelectedProduct.Id;
                var success = await _productService.DeleteProductAsync(productId);
                if (success)
                {
                    Products.Remove(SelectedProduct);
                    SuccessMessage = "Product deleted successfully.";
                    SelectedProduct = null;
                }
                else { ErrorMessage = "Failed to delete product."; }
            }
            catch (Exception ex) { ErrorMessage = $"Error deleting product: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private bool CanExecuteDeleteProduct() => SelectedProduct != null && !IsBusy;

        // ── Submit handlers (called by view after dialog OK) ──────────────────

        /// <summary>
        /// Called by ProductsView after the Create dialog is confirmed.
        /// Assumes ProductService.CreateProductAsync(ProductDto) exists.
        /// </summary>
        public async Task SubmitCreateProductAsync(ProductDto dto)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                await _productService.CreateProductAsync(dto);
                SuccessMessage = "Product aangemaakt.";
                ExecuteLoadProducts();
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij aanmaken: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Called by ProductsView after the Edit dialog is confirmed.
        /// Assumes ProductService.UpdateProductAsync(ProductDto) exists.
        /// </summary>
        public async Task SubmitUpdateProductAsync(ProductDto dto)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                await _productService.UpdateProductAsync(dto);
                SuccessMessage = "Product bijgewerkt.";
                ExecuteLoadProducts();
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij bijwerken: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}