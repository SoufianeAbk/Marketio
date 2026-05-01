using System.Windows;
using Marketio_WPF.ViewModels;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Bind the password box to the ViewModel
            if (DataContext is LoginViewModel viewModel)
            {
                // Subscribe to navigation events
                viewModel.LoginSucceeded += ViewModel_LoginSucceeded;
                viewModel.RegisterRequested += ViewModel_RegisterRequested;
            }
        }

        private void ViewModel_LoginSucceeded(object? sender, EventArgs e)
        {
            // Close the login window
            DialogResult = true;
            Close();
        }

        private void ViewModel_RegisterRequested(object? sender, EventArgs e)
        {
            // Navigate to register view
            var registerView = new RegisterView();
            registerView.Show();
            // Close login window
            Close();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe to prevent memory leaks
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSucceeded -= ViewModel_LoginSucceeded;
                viewModel.RegisterRequested -= ViewModel_RegisterRequested;
            }
        }
    }
}