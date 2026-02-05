using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class UsersPage : Page
    {
        public UsersViewModel ViewModel { get; }
        public UsersPage(UsersViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
