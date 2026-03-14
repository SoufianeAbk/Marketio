using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class OrdersPage : ContentPage
    {
        public OrdersPage(OrdersViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is OrdersViewModel viewModel)
            {
                await viewModel.LoadOrdersCommand.ExecuteAsync(null);
            }
        }
    }
}