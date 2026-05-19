using Marketio_WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views
{
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
            Loaded += RegisterView_Loaded;
        }

        private void RegisterView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not RegisterViewModel viewModel)
                return;

            if (FindName("PasswordBox") is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged += (_, _) =>
                {
                    viewModel.Password = passwordBox.Password;
                };
            }

            if (FindName("ConfirmPasswordBox") is PasswordBox confirmPasswordBox)
            {
                confirmPasswordBox.PasswordChanged += (_, _) =>
                {
                    viewModel.ConfirmPassword = confirmPasswordBox.Password;
                };
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}