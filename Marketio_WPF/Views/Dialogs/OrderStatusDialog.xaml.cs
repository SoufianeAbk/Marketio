using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views.Dialogs
{
    public partial class OrderStatusDialog : Window
    {
        /// <summary>The status name chosen by the user (e.g. "Shipped").</summary>
        public string SelectedStatus { get; private set; } = "Pending";

        public OrderStatusDialog(dynamic order)
        {
            InitializeComponent();

            OrderNumberText.Text = $"Order:   {order.OrderNumber}";
            OrderDateText.Text = $"Datum:   {order.OrderDate:dd/MM/yyyy}";
            OrderTotalText.Text = $"Totaal:  €{order.TotalAmount:N2}";
            CurrentStatusText.Text = $"Huidig:  {order.StatusName}";

            string current = order.StatusName?.ToString() ?? "Pending";
            foreach (ComboBoxItem item in StatusBox.Items)
                if (item.Content.ToString() == current) { StatusBox.SelectedItem = item; break; }

            if (StatusBox.SelectedItem == null)
                StatusBox.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatus = ((ComboBoxItem)StatusBox.SelectedItem).Content.ToString()!;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}