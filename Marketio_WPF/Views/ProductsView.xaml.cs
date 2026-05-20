using Marketio_WPF.ViewModels;
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
            if (DataContext is ProductsViewModel vm)
            {
                vm.LoadProductsCommand.Execute(null);
            }
        }
    }
}