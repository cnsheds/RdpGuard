using System.Windows;

namespace OpenRdpGuard.Views.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, bool showCancel = true)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
            CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
