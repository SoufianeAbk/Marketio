using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views.Dialogs
{
    public partial class ProductDialog : Window
    {
        public ProductDialog(dynamic? product = null)
        {
            InitializeComponent();

            if (product != null)
            {
                TitleText.Text = "✏️ Product bewerken";
                NameBox.Text = product.Name?.ToString() ?? string.Empty;
                DescriptionBox.Text = product.Description?.ToString() ?? string.Empty;
                PriceBox.Text = product.Price?.ToString() ?? "0";
                StockBox.Text = product.Stock?.ToString() ?? "0";
                ImageUrlBox.Text = product.ImageUrl?.ToString() ?? string.Empty;
                IsActiveCheck.IsChecked = product.IsActive == true;

                int catValue = (int)product.Category;
                foreach (ComboBoxItem item in CategoryBox.Items)
                    if (Convert.ToInt32(item.Tag) == catValue) { CategoryBox.SelectedItem = item; break; }
            }
            else
            {
                CategoryBox.SelectedIndex = 0;
            }
        }

        /// <summary>Bouwt een ProductDto uit de huidige formulierwaarden.</summary>
        public ProductDto ToDto(int id = 0)
        {
            var selected = (ComboBoxItem)CategoryBox.SelectedItem;
            return new ProductDto
            {
                Id = id,
                Name = NameBox.Text.Trim(),
                Description = DescriptionBox.Text.Trim(),
                Price = decimal.TryParse(PriceBox.Text, out var p) ? p : 0m,
                Stock = int.TryParse(StockBox.Text, out var s) ? s : 0,
                Category = (ProductCategory)Convert.ToInt32(selected.Tag),
                ImageUrl = ImageUrlBox.Text.Trim(),
                IsActive = IsActiveCheck.IsChecked == true
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            { ShowError("Naam is verplicht."); return; }

            if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
            { ShowError("Beschrijving is verplicht."); return; }

            if (!decimal.TryParse(PriceBox.Text, out var price) || price <= 0)
            { ShowError("Voer een geldige prijs in (> 0)."); return; }

            if (!int.TryParse(StockBox.Text, out var stock) || stock < 0)
            { ShowError("Voer een geldige voorraad in (≥ 0)."); return; }

            if (CategoryBox.SelectedItem == null)
            { ShowError("Selecteer een categorie."); return; }

            if (string.IsNullOrWhiteSpace(ImageUrlBox.Text))
            { ShowError("Afbeelding-URL is verplicht."); return; }

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