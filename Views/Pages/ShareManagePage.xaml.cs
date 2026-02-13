using OpenRdpGuard.ViewModels;
using System.Windows.Controls;

namespace OpenRdpGuard.Views.Pages
{
    public partial class ShareManagePage : Page
    {
        public ShareManagePage(ShareManageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
