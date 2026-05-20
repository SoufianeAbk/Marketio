using System.Windows.Controls;
using Marketio_WPF.ViewModels;

namespace Marketio_WPF.Views
{
    /// <summary>
    /// Interaction logic voor AdminView.xaml
    /// </summary>
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm)
            {
                vm.LoadUsersCommand.Execute(null);
                vm.LoadRolesCommand.Execute(null);
            }
        }
    }
}