using Marketio_App.ViewModels;

namespace Marketio_App.Pages
{
    public partial class ProductDetailPage : ContentPage
    {
        public ProductDetailPage(ProductDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}