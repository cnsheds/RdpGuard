using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OpenRdpGuard.ViewModels
{
    public partial class ConnectionsViewModel : ObservableObject
    {
        private readonly IConnectionService _connectionService;
        private readonly IFirewallService _firewallService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private ObservableCollection<ConnectionInfo> _connections = new();

        [ObservableProperty]
        private bool _onlyRdpConnections = true;

        public ConnectionsViewModel(IConnectionService connectionService, IFirewallService firewallService, ISettingsService settingsService)
        {
            _connectionService = connectionService;
            _firewallService = firewallService;
            _settingsService = settingsService;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            var data = await _connectionService.GetActiveConnectionsAsync();
            var blocked = await _firewallService.GetBlockedIpsAsync();
            var blockedSet = blocked.ToHashSet();
            var rdpPort = _settingsService.GetRdpPort();

            foreach (var conn in data)
            {
                var remote = conn.RemoteAddress.Split(':')[0];
                conn.IsBlacklisted = blockedSet.Contains(remote);
            }

            var filtered = OnlyRdpConnections
                ? data.Where(c => IsRdpConnection(c, rdpPort))
                : data;

            Connections = new ObservableCollection<ConnectionInfo>(filtered);
        }

        [RelayCommand]
        private async Task KillConnection(ConnectionInfo conn)
        {
            if (conn != null)
            {
                await _connectionService.KillConnectionAsync(conn.Pid);
                await Refresh();
            }
        }

        partial void OnOnlyRdpConnectionsChanged(bool value)
        {
            _ = Refresh();
        }

        private static bool IsRdpConnection(ConnectionInfo conn, int rdpPort)
        {
            if (rdpPort <= 0) return false;
            var suffix = ":" + rdpPort;
            return conn.LocalAddress.EndsWith(suffix) || conn.RemoteAddress.EndsWith(suffix);
        }
    }
}
