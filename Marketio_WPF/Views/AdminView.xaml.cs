using System.Windows.Controls;
using Marketio_WPF.ViewModels;

namespace Marketio_WPF.Views
{
    /// <summary>
    /// Interactie logica voor AdminView.xaml
    /// </summary>
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();
        }

        // Gebruik InitializeAsync() zodat rollen en gebruikers SEQUENTIEEL worden geladen.
        // Het apart aanroepen van LoadUsersCommand + LoadRolesCommand startte twee async-void
        // methodes tegelijkertijd op dezelfde DbContext-instantie, wat leidde tot:
        //   InvalidOperationException: A second operation was started on this context instance
        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
