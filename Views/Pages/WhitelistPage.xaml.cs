using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class WhitelistPage : Page
    {
        public WhitelistViewModel ViewModel { get; }
        public WhitelistPage(WhitelistViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
