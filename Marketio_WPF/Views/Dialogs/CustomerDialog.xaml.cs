using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views.Dialogs
{
    public partial class CustomerDialog : Window
    {
        public string FirstName => FirstNameBox.Text.Trim();
        public string LastName => LastNameBox.Text.Trim();
        public string Email => EmailBox.Text.Trim();
        public string PhoneNumber => PhoneBox.Text.Trim();
        public string Address => AddressBox.Text.Trim();
        public bool IsActive => IsActiveCheck.IsChecked == true;

        public CustomerDialog(dynamic? customer = null)
        {
            InitializeComponent();

            if (customer != null)
            {
                TitleText.Text = "✏️ Klant bewerken";
                FirstNameBox.Text = customer.FirstName?.ToString() ?? string.Empty;
                LastNameBox.Text = customer.LastName?.ToString() ?? string.Empty;
                EmailBox.Text = customer.Email?.ToString() ?? string.Empty;
                PhoneBox.Text = customer.PhoneNumber?.ToString() ?? string.Empty;
                AddressBox.Text = customer.Address?.ToString() ?? string.Empty;
                IsActiveCheck.IsChecked = customer.IsActive == true;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            { ShowError("Voornaam is verplicht."); return; }
            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
            { ShowError("Achternaam is verplicht."); return; }
            if (string.IsNullOrWhiteSpace(EmailBox.Text) || !EmailBox.Text.Contains('@'))
            { ShowError("Geldig e-mailadres is verplicht."); return; }
            if (string.IsNullOrWhiteSpace(PhoneBox.Text))
            { ShowError("Telefoonnummer is verplicht."); return; }
            if (string.IsNullOrWhiteSpace(AddressBox.Text))
            { ShowError("Adres is verplicht."); return; }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}