using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class ProductsPage : ContentPage
    {
        public ProductsPage(ProductsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ProductsViewModel viewModel)
            {
                await viewModel.LoadProductsCommand.ExecuteAsync(null);
            }
        }
    }
}