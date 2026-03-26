using Marketio_App.ViewModels;
using Marketio_Shared.DTOs;

namespace Marketio_App.Pages
{
    public partial class ProductsPage : ContentPage
    {
        private bool _isInitialized = false;

        public ProductsPage(ProductsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Only load once to prevent duplicate requests
            if (_isInitialized)
                return;

            _isInitialized = true;

            if (BindingContext is ProductsViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("[ProductsPage] OnAppearing - executing LoadProductsCommand");
                await viewModel.LoadProductsCommand.ExecuteAsync(null);
                System.Diagnostics.Debug.WriteLine($"[ProductsPage] OnAppearing - LoadProductsCommand completed. Products count: {viewModel.Products.Count}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isInitialized = false;
        }
    }
}