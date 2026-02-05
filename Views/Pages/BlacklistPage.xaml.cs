using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class BlacklistPage : Page
    {
        public BlacklistViewModel ViewModel { get; }
        public BlacklistPage(BlacklistViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
