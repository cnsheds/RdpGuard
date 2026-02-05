using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class ConnectionsPage : Page
    {
        public ConnectionsViewModel ViewModel { get; }
        public ConnectionsPage(ConnectionsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
