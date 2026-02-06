using Microsoft.Win32;
using OpenRdpGuard.Helpers;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface ISettingsService
    {
        int GetRdpPort();
        Task<bool> SetRdpPortAsync(int port);
    }

    public class SettingsService : ISettingsService
    {
        private const string RdpKeyPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";
        private const string PortValueName = "PortNumber";
        private const string AllowRuleName = FirewallRuleNames.AllowRule;
        private const string AllowRuleNameUdp = FirewallRuleNames.AllowRuleUdp;
        private readonly FirewallComManager _comManager = new FirewallComManager(FirewallRuleNames.BlockRule, AllowRuleName, AllowRuleNameUdp);

        public int GetRdpPort()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RdpKeyPath);
                if (key?.GetValue(PortValueName) is int port)
                {
                    return port;
                }
            }
            catch { }
            return 3389;
        }

        public async Task<bool> SetRdpPortAsync(int port)
        {
            if (port < 1024 || port > 65535) return false;

            var oldPort = GetRdpPort();
            await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(RdpKeyPath, true);
                    key?.SetValue(PortValueName, port, RegistryValueKind.DWord);
                    UpdateFirewallRules(oldPort, port);
                }
                catch { }
            });

            return true;
        }

        private void UpdateFirewallRules(int oldPort, int newPort)
        {
            _comManager.DeleteAllowRules();
            _comManager.UpsertAllowRule(AllowRuleName, FirewallComManager.ProtocolTcp, newPort, "Any", true);
            _comManager.UpsertAllowRule(AllowRuleNameUdp, FirewallComManager.ProtocolUdp, newPort, "Any", true);
        }
    }
}
