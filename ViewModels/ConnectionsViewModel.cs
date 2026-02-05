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

        [ObservableProperty]
        private ObservableCollection<ConnectionInfo> _connections = new();

        public ConnectionsViewModel(IConnectionService connectionService, IFirewallService firewallService)
        {
            _connectionService = connectionService;
            _firewallService = firewallService;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            var data = await _connectionService.GetActiveConnectionsAsync();
            var blocked = await _firewallService.GetBlockedIpsAsync();
            var blockedSet = blocked.ToHashSet();

            foreach (var conn in data)
            {
                var remote = conn.RemoteAddress.Split(':')[0];
                conn.IsBlacklisted = blockedSet.Contains(remote);
            }

            Connections = new ObservableCollection<ConnectionInfo>(data);
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
    }
}
