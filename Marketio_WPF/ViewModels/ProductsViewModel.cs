using CommunityToolkit.Mvvm.Input;
using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using Marketio_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;

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

        // Categorie-filter
        private List<dynamic> _allProducts = new();
        private string _selectedCategoryFilter = "Alle categorieën";

        /// <summary>
        /// Vaste lijst voor de ComboBox: "Alle categorieën" + elke enum-waarde.
        /// </summary>
        public IReadOnlyList<string> CategoryFilterOptions { get; } =
            new[] { "Alle categorieën" }
            .Concat(Enum.GetNames<ProductCategory>())
            .ToArray();

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                    ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            Products = _selectedCategoryFilter == "Alle categorieën"
                ? new ObservableCollection<dynamic>(_allProducts)
                : new ObservableCollection<dynamic>(
                    _allProducts.Where(p =>
                        ((ProductCategory)p.Category).ToString() == _selectedCategoryFilter));
        }

        // Dialoogvenster-gebeurtenissen
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
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    EditProductCommand.NotifyCanExecuteChanged();
                    DeleteProductCommand.NotifyCanExecuteChanged();
                }
            }
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

        // Laden
        private async void ExecuteLoadProducts()
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                var products = await _productService.GetAllProductsAsync();
                _allProducts = products ?? new List<dynamic>();
                ApplyFilter();
                if (!Products.Any())
                    ErrorMessage = "No products found.";
            }
            catch (Exception ex) { ErrorMessage = $"Error loading products: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // Aanmaken / Bewerken (activeert gebeurtenissen; de view opent een dialoogvenster)
        private void ExecuteCreateProduct() =>
            CreateProductRequested?.Invoke(this, EventArgs.Empty);

        private void ExecuteEditProduct()
        {
            if (SelectedProduct == null) { ErrorMessage = "No product selected."; return; }
            EditProductRequested?.Invoke(this, SelectedProduct);
        }

        private bool CanExecuteEditProduct() => SelectedProduct != null && !IsBusy;

        // Verwijderen
        private async void ExecuteDeleteProduct()
        {
            if (SelectedProduct == null) { ErrorMessage = "No product selected."; return; }

            var bevestiging = MessageBox.Show(
                $"Weet u zeker dat u '{SelectedProduct.Name}' wilt verwijderen?",
                "Product verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (bevestiging != MessageBoxResult.Yes) return;

            try
            {
                IsBusy = true;
                ClearMessages();
                var productId = (int)SelectedProduct.Id;
                var success = await _productService.DeleteProductAsync(productId);
                if (success)
                {
                    _allProducts.Remove(SelectedProduct);
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

        // Verwerkingsmethoden (aangeroepen door de view na bevestiging van het dialoogvenster)

        /// <summary>
        /// Aangeroepen door ProductsView nadat het dialoogvenster voor aanmaken is bevestigd.
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
        /// Aangeroepen door ProductsView nadat het dialoogvenster voor bewerken is bevestigd.
        /// OPLOSSING CS7036: dto.Id meegeven als eerste argument omdat ProductService.UpdateProductAsync
        /// verwacht (int productId, dynamic productData).
        /// </summary>
        public async Task SubmitUpdateProductAsync(ProductDto dto)
        {
            try
            {
                IsBusy = true;
                ClearMessages();
                // dto.Id als eerste argument + dto als productData
                await _productService.UpdateProductAsync(dto.Id, dto);
                SuccessMessage = "Product bijgewerkt.";
                ExecuteLoadProducts();
            }
            catch (Exception ex) { ErrorMessage = $"Fout bij bijwerken: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}