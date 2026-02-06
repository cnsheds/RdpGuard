using OpenRdpGuard.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private static readonly Regex NumericOnlyRegex = new Regex("^[0-9]+$");

        private void MonitorInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumericOnlyRegex.IsMatch(e.Text);
        }

        private void MonitorInterval_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
            {
                var text = e.DataObject.GetData(System.Windows.DataFormats.Text) as string;
                if (string.IsNullOrWhiteSpace(text) || !NumericOnlyRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
