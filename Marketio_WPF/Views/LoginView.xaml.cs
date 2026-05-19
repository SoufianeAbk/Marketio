using Marketio_WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();

            // Wire up after InitializeComponent so named elements exist
            Loaded += (s, e) =>
            {
                if (DataContext is not LoginViewModel viewModel)
                    return;

                // Subscribe to password changes to update ViewModel
                PasswordBox.PasswordChanged += (_, _) =>
                {
                    viewModel.Password = PasswordBox.Password;
                };

                // Subscribe to navigation events
                viewModel.LoginSucceeded += (_, _) =>
                {
                    DialogResult = true;
                    Close();
                };

                viewModel.RegisterRequested += (_, _) =>
                {
                    try
                    {
                        var registerViewModel = App.ServiceProvider.GetRequiredService<RegisterViewModel>();
                        var registerView = new RegisterView { DataContext = registerViewModel };
                        registerView.Owner = this;
                        registerView.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error opening registration window: {ex.Message}",
                            "Registration Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    // Close login view after showing register
                    Close();
                };
            };
        }
    }
}