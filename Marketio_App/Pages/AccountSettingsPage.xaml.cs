using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class AccountSettingsPage : ContentPage
    {
        public AccountSettingsPage(AccountSettingsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var viewModel = (AccountSettingsViewModel)BindingContext;
            await viewModel.LoadProfileCommand.ExecuteAsync(null);
        }
    }
}