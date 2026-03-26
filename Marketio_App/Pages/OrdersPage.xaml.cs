using Marketio_App.Services;
using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class OrdersPage : ContentPage
    {
        private readonly AuthService _authService;

        public OrdersPage(OrdersViewModel viewModel, AuthService authService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _authService = authService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if user is authenticated
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                // User not logged in, redirect to login
                await Shell.Current.GoToAsync("///login");
                return;
            }

            // User is authenticated, load orders
            if (BindingContext is OrdersViewModel viewModel)
            {
                await viewModel.LoadOrdersCommand.ExecuteAsync(null);
            }
        }
    }
}