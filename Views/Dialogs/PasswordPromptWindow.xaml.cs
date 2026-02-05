using System.Windows;

namespace OpenRdpGuard.Views.Dialogs
{
    public partial class PasswordPromptWindow : Window
    {
        public string Password { get; private set; } = string.Empty;

        public PasswordPromptWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
