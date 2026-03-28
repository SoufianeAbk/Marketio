using Marketio_App.Pages;

namespace Marketio_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register detail routes programmatically
            // QueryProperties only work with routes registered via Routing.RegisterRoute()
            Routing.RegisterRoute("product-detail", typeof(ProductDetailPage));
            Routing.RegisterRoute("order-detail", typeof(OrderDetailPage));
            Routing.RegisterRoute("create-order", typeof(CreateOrderPage));
        }
    }
}