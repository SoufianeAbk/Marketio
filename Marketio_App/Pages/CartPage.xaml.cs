using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class CartPage : ContentPage
    {
        public CartPage(CartViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is CartViewModel viewModel)
            {
                await viewModel.LoadCartCommand.ExecuteAsync(null);
            }
        }
    }
}