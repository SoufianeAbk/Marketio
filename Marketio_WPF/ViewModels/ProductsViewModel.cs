using CommunityToolkit.Mvvm.Input;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;

namespace Marketio_WPF.ViewModels
{
    /// <summary>
    /// ViewModel for managing products.
    /// Handles product listing, filtering, and CRUD operations.
    /// </summary>
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

        private async void ExecuteLoadProducts()
        {
            try
            {
                IsBusy = true;
                ClearMessages();

                var products = await _productService.GetAllProductsAsync();
                Products = new ObservableCollection<dynamic>(products ?? new List<dynamic>());

                if (!Products.Any())
                {
                    ErrorMessage = "No products found.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading products: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteCreateProduct()
        {
            // Navigation and dialog would be handled by view
        }

        private void ExecuteEditProduct()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "No product selected.";
                return;
            }

            // Navigation and dialog would be handled by view
        }

        private bool CanExecuteEditProduct()
        {
            return SelectedProduct != null && !IsBusy;
        }

        private async void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "No product selected.";
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                // Assuming the product has an Id property
                var productId = (int)SelectedProduct.Id;
                var success = await _productService.DeleteProductAsync(productId);

                if (success)
                {
                    Products.Remove(SelectedProduct);
                    SuccessMessage = "Product deleted successfully.";
                    SelectedProduct = null;
                }
                else
                {
                    ErrorMessage = "Failed to delete product.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting product: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteDeleteProduct()
        {
            return SelectedProduct != null && !IsBusy;
        }
    }
}