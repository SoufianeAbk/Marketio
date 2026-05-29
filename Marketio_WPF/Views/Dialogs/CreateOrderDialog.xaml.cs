using Marketio_Shared.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views.Dialogs
{
    public partial class CreateOrderDialog : Window
    {
        // ── Data ─────────────────────────────────────────────────────────────
        private readonly List<dynamic> _products;

        /// <summary>Artikelrijen; ItemsControl bindt hieraan via code-behind.</summary>
        public ObservableCollection<OrderItemRow> Items { get; } = new();

        // ── Constructor ───────────────────────────────────────────────────────
        public CreateOrderDialog(List<dynamic> customers, List<dynamic> products)
        {
            InitializeComponent();
            _products = products;

            // Klanten in ComboBox
            CustomerBox.ItemsSource = customers;

            // Betaalmethoden in ComboBox (met Nederlandstalige labels)
            PaymentBox.ItemsSource = new[]
            {
                new PaymentOption(PaymentMethod.CreditCard,    "Kredietkaart"),
                new PaymentOption(PaymentMethod.DebitCard,     "Betaalkaart"),
                new PaymentOption(PaymentMethod.PayPal,        "PayPal"),
                new PaymentOption(PaymentMethod.BankTransfer,  "Bankoverschrijving"),
                new PaymentOption(PaymentMethod.Cash,          "Contant"),
            };
            PaymentBox.DisplayMemberPath = "Display";
            PaymentBox.SelectedValuePath = "Value";
            PaymentBox.SelectedIndex = 0;

            // Artikelen DataSource
            ItemsList.ItemsSource = Items;
            Items.CollectionChanged += (_, _) => UpdateTotal();

            // Eerste lege rij
            AddRow();
        }

        // ── Publieke uitleeswaarden (gebruikt door OrdersView.xaml.cs) ────────

        public string SelectedCustomerId
        {
            get
            {
                if (CustomerBox.SelectedItem == null) return string.Empty;
                dynamic c = CustomerBox.SelectedItem;
                return c.Id?.ToString() ?? string.Empty;
            }
        }

        public string ShippingAddress => ShippingBox.Text.Trim();

        public string BillingAddress => BillingBox.Text.Trim();

        public PaymentMethod SelectedPaymentMethod =>
            PaymentBox.SelectedValue != null
                ? (PaymentMethod)PaymentBox.SelectedValue
                : PaymentMethod.CreditCard;

        /// <summary>Geeft alleen de complete, geldige rijen terug.</summary>
        public List<(int ProductId, int Quantity, decimal UnitPrice)> OrderItems
        {
            get
            {
                var result = new List<(int, int, decimal)>();
                foreach (var row in Items)
                {
                    if (row.SelectedProduct == null || row.Quantity < 1) continue;
                    int productId = (int)row.SelectedProduct.Id;
                    result.Add((productId, row.Quantity, row.UnitPrice));
                }
                return result;
            }
        }

        // ── Rijen beheer ──────────────────────────────────────────────────────

        private void AddRow()
        {
            var row = new OrderItemRow(_products);
            row.PropertyChanged += (_, _) => UpdateTotal();
            Items.Add(row);
        }

        private void AddItem_Click(object sender, RoutedEventArgs e) => AddRow();

        private void RemoveItemRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderItemRow row)
                Items.Remove(row);
            UpdateTotal();
        }

        // ── Totaalberekening ─────────────────────────────────────────────────

        private void UpdateTotal()
        {
            var total = Items.Sum(r => r.LineTotal);
            TotalText.Text = $"Totaal: €{total:N2}";
        }

        // ── "Zelfde als verzendadres"-checkbox ────────────────────────────────

        private void SameAsShipping_Changed(object sender, RoutedEventArgs e)
        {
            if (SameAsShippingCheck.IsChecked == true)
            {
                BillingBox.Text = ShippingBox.Text;
                BillingBox.IsEnabled = false;
            }
            else
            {
                BillingBox.IsEnabled = true;
            }
        }

        private void ShippingBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SameAsShippingCheck?.IsChecked == true)
                BillingBox.Text = ShippingBox.Text;
        }

        // ── Opslaan / Annuleren ───────────────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerBox.SelectedItem == null)
            { ShowError("Selecteer een klant."); return; }

            if (string.IsNullOrWhiteSpace(ShippingBox.Text))
            { ShowError("Verzendadres is verplicht."); return; }

            if (string.IsNullOrWhiteSpace(BillingBox.Text))
            { ShowError("Factuuradres is verplicht."); return; }

            if (!Items.Any())
            { ShowError("Voeg minstens één artikel toe."); return; }

            if (Items.Any(r => r.SelectedProduct == null))
            { ShowError("Elk artikel moet een product hebben."); return; }

            if (Items.Any(r => r.Quantity < 1))
            { ShowError("Elke hoeveelheid moet minimaal 1 zijn."); return; }

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

    // ── Hulpklassen ───────────────────────────────────────────────────────────

    /// <summary>
    /// Eén artikelrij in de CreateOrderDialog.
    /// Implementeert INotifyPropertyChanged zodat UnitPrice en LineTotal
    /// automatisch bijwerken zodra de gebruiker een product of hoeveelheid kiest.
    /// </summary>
    public class OrderItemRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly List<dynamic> _allProducts;
        private dynamic? _selectedProduct;
        private int _quantity = 1;

        public List<dynamic> Products => _allProducts;

        public dynamic? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                Notify();
                Notify(nameof(UnitPrice));
                Notify(nameof(LineTotal));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value < 1 ? 1 : value;
                Notify();
                Notify(nameof(LineTotal));
            }
        }

        /// <summary>Eenheidsprijs overgenomen uit het geselecteerde product.</summary>
        public decimal UnitPrice =>
            _selectedProduct != null ? (decimal)_selectedProduct.Price : 0m;

        /// <summary>Quantity × UnitPrice.</summary>
        public decimal LineTotal => UnitPrice * Quantity;

        public OrderItemRow(List<dynamic> products)
        {
            _allProducts = products;
        }

        private void Notify([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Interne wrapper voor de betaalmethode-ComboBox.</summary>
    internal record PaymentOption(PaymentMethod Value, string Display);
}
