using System.Windows;
using Marketio_WPF.ViewModels;

namespace Marketio_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set the DataContext to MainViewModel
            this.DataContext = App.ServiceProvider.GetService(typeof(MainViewModel));
        }
    }
}