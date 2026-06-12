using Marketio_WPF.ViewModels;
using Marketio_WPF.Views.Dialogs;
using Marketio_Shared.Enums;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not OrdersViewModel vm)
                return;

            vm.LoadOrdersCommand.Execute(null);

            // Nieuwe order aanmaken
            vm.CreateOrderRequested += async (_, payload) =>
            {
                var dialog = new CreateOrderDialog(payload.Customers, payload.Products)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await vm.SubmitCreateOrderAsync(
                        dialog.SelectedCustomerId,
                        dialog.ShippingAddress,
                        dialog.BillingAddress,
                        dialog.SelectedPaymentMethod,
                        dialog.OrderItems
                    );
                }
            };

            // Orderstatus bijwerken
            vm.UpdateOrderRequested += async (_, order) =>
            {
                var dialog = new OrderStatusDialog(order)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await vm.SubmitStatusUpdateAsync(
                        (int)order.Id,
                        dialog.SelectedStatus
                    );
                }
            };
        }
    }
}
