using System.Windows;
using Marketio_WPF.ViewModels;

namespace Marketio_WPF
{
    /// <summary>
    /// Interactie logica voor MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set the DataContext naar MainViewModel
            this.DataContext = App.ServiceProvider.GetService(typeof(MainViewModel));
        }
    }
}