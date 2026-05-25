using Marketio_WPF.ViewModels;
using Marketio_WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Marketio_WPF.Views
{
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProductsViewModel vm) return;

            vm.LoadProductsCommand.Execute(null);

            vm.CreateProductRequested += (_, _) =>
            {
                var dialog = new ProductDialog { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                    _ = vm.SubmitCreateProductAsync(dialog.ToDto());
            };

            vm.EditProductRequested += (_, product) =>
            {
                var dialog = new ProductDialog(product) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                    _ = vm.SubmitUpdateProductAsync(dialog.ToDto((int)product.Id));
            };
        }
    }
}