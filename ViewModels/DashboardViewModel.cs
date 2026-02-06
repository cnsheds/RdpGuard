using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Threading.Tasks;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;

namespace OpenRdpGuard.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IFirewallService _firewallService;
        private readonly IWhitelistService _whitelistService;
        private readonly IConnectionService _connectionService;
        private readonly ISystemService _systemService;

        [ObservableProperty]
        private int _rdpPort = 3389;

        [ObservableProperty]
        private string _firewallStatusText = "未管理";

        [ObservableProperty]
        private System.Windows.Media.Brush _firewallStatusBrush = MediaBrushes.Gray;

        [ObservableProperty]
        private string _whitelistStatusText = "Any";

        [ObservableProperty]
        private int _connectionCount = 0;

        [ObservableProperty]
        private int _blockedCount = 0;

        [ObservableProperty]
        private string _rdpServiceStatusText = "未知";

        [ObservableProperty]
        private System.Windows.Media.Brush _rdpServiceStatusBrush = MediaBrushes.Gray;

        [ObservableProperty]
        private string _firewallServiceStatusText = "未知";

        [ObservableProperty]
        private System.Windows.Media.Brush _firewallServiceStatusBrush = MediaBrushes.Gray;

        public DashboardViewModel(ISettingsService settingsService,
            IFirewallService firewallService,
            IWhitelistService whitelistService,
            IConnectionService connectionService,
            ISystemService systemService)
        {
            _settingsService = settingsService;
            _firewallService = firewallService;
            _whitelistService = whitelistService;
            _connectionService = connectionService;
            _systemService = systemService;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            RdpPort = _settingsService.GetRdpPort();

            var isManaged = _firewallService.IsManaged();
            FirewallStatusText = isManaged ? "已由 RdpGuard 管理" : "未管理";
            FirewallStatusBrush = isManaged ? MediaBrushes.Green : MediaBrushes.Gray;

            var rdpRunning = _systemService.IsRdpServiceRunning();
            RdpServiceStatusText = rdpRunning ? "运行中" : "已停止";
            RdpServiceStatusBrush = rdpRunning ? MediaBrushes.Green : MediaBrushes.Red;

            var fwRunning = _systemService.IsFirewallServiceRunning();
            FirewallServiceStatusText = fwRunning ? "运行中" : "已停止";
            FirewallServiceStatusBrush = fwRunning ? MediaBrushes.Green : MediaBrushes.Red;

            WhitelistStatusText = _whitelistService.IsWhitelistEnabled() ? "仅白名单" : "Any";

            try
            {
                var connections = await _connectionService.GetActiveConnectionsAsync();
                var suffix = ":" + RdpPort;
                ConnectionCount = connections.Count(c => c.LocalAddress.EndsWith(suffix) || c.RemoteAddress.EndsWith(suffix));
            }
            catch
            {
                ConnectionCount = 0;
            }

            try
            {
                var blocked = await _firewallService.GetBlockedIpsAsync();
                BlockedCount = blocked.Count;
            }
            catch
            {
                BlockedCount = 0;
            }
        }
    }
}
