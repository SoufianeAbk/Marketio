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

            // Refresh profile data to ensure we have the current user's data
            // This is especially important after login with a different user
            await viewModel.RefreshProfileDataCommand.ExecuteAsync(null);
        }
    }
}