using CommunityToolkit.Mvvm.ComponentModel;
using OpenRdpGuard.Models;
using OpenRdpGuard.Views.Pages;
using System.Collections.ObjectModel;
using System.Security.Principal;

namespace OpenRdpGuard.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "RdpGuard - 远程桌面安全防护";

        [ObservableProperty]
        private string _appVersion = "1.26.2.13";

        [ObservableProperty]
        private ObservableCollection<NavigationItem> _functionItems;

        [ObservableProperty]
        private ObservableCollection<NavigationItem> _settingsItems;

        public MainWindowViewModel()
        {
            if (IsRunningAsAdministrator())
            {
                ApplicationTitle += " (管理员)";
            }

            _functionItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Title = "当前状态", IconGlyph = "\uE9D9", PageType = typeof(DashboardPage) },
                new NavigationItem { Title = "登录统计", IconGlyph = "\uE9D2", PageType = typeof(LoginStatsPage) },
                new NavigationItem { Title = "实时连接", IconGlyph = "\uE774", PageType = typeof(ConnectionsPage) }
            };

            _settingsItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Title = "端口配置", IconGlyph = "\uE7C9", PageType = typeof(PortConfigPage) },
                new NavigationItem { Title = "IP黑名单", IconGlyph = "\uE8C7", PageType = typeof(BlacklistPage) },
                new NavigationItem { Title = "IP白名单", IconGlyph = "\uE8F1", PageType = typeof(WhitelistPage) },
                new NavigationItem { Title = "用户管理", IconGlyph = "\uE77B", PageType = typeof(UsersPage) },
                new NavigationItem { Title = "共享管理", IconGlyph = "\uE8A7", PageType = typeof(ShareManagePage) },
                new NavigationItem { Title = "恢复默认", IconGlyph = "\uE777", PageType = typeof(RestoreDefaultsPage) },
                new NavigationItem { Title = "主题设置", IconGlyph = "\uE790", PageType = typeof(ThemeSettingsPage) }
            };
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null)
                {
                    return false;
                }

                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
