using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is LoginViewModel viewModel)
            {
                await viewModel.LoadRememberedEmailCommand.ExecuteAsync(null);
            }
        }
    }
}