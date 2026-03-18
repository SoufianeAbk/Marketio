using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class CreateOrderPage : ContentPage
    {
        public CreateOrderPage(CreateOrderViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is CreateOrderViewModel viewModel)
            {
                await viewModel.LoadCartCommand.ExecuteAsync(null);
            }
        }
    }
}