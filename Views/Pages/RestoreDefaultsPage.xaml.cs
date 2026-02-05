using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class RestoreDefaultsPage : Page
    {
        public RestoreDefaultsViewModel ViewModel { get; }
        public RestoreDefaultsPage(RestoreDefaultsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
