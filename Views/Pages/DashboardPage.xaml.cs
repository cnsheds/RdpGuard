using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
