using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class LoginStatsPage : Page
    {
        public LoginStatsPage(LoginStatsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
