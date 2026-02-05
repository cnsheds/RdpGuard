using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Threading.Tasks;
using System.Windows.Media;

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
        private Brush _firewallStatusBrush = Brushes.Gray;

        [ObservableProperty]
        private string _whitelistStatusText = "Any";

        [ObservableProperty]
        private int _connectionCount = 0;

        [ObservableProperty]
        private int _blockedCount = 0;

        [ObservableProperty]
        private string _rdpServiceStatusText = "未知";

        [ObservableProperty]
        private Brush _rdpServiceStatusBrush = Brushes.Gray;

        [ObservableProperty]
        private string _firewallServiceStatusText = "未知";

        [ObservableProperty]
        private Brush _firewallServiceStatusBrush = Brushes.Gray;

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
            FirewallStatusBrush = isManaged ? Brushes.Green : Brushes.Gray;

            var rdpRunning = _systemService.IsRdpServiceRunning();
            RdpServiceStatusText = rdpRunning ? "运行中" : "已停止";
            RdpServiceStatusBrush = rdpRunning ? Brushes.Green : Brushes.Red;

            var fwRunning = _systemService.IsFirewallServiceRunning();
            FirewallServiceStatusText = fwRunning ? "运行中" : "已停止";
            FirewallServiceStatusBrush = fwRunning ? Brushes.Green : Brushes.Red;

            WhitelistStatusText = _whitelistService.IsWhitelistEnabled() ? "仅白名单" : "Any";

            try
            {
                var connections = await _connectionService.GetActiveConnectionsAsync();
                ConnectionCount = connections.Count;
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
