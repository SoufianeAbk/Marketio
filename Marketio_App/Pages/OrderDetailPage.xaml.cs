using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class OrderDetailPage : ContentPage
    {
        public OrderDetailPage(OrderDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is OrderDetailViewModel viewModel)
            {
                await viewModel.LoadOrderCommand.ExecuteAsync(viewModel.OrderId);
            }
        }
    }
}