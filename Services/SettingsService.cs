using Microsoft.Win32;
using System.Diagnostics;
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
        private const string AllowRuleName = "OpenRdpGuard-RDP-Allow";
        private const string AllowRuleNameUdp = "OpenRdpGuard-RDP-Allow-UDP";

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
            RunNetsh($"advfirewall firewall delete rule name=\"{AllowRuleName}\"");
            RunNetsh($"advfirewall firewall delete rule name=\"{AllowRuleNameUdp}\"");

            RunNetsh($"advfirewall firewall add rule name=\"{AllowRuleName}\" dir=in action=allow protocol=TCP localport={newPort}");
            RunNetsh($"advfirewall firewall add rule name=\"{AllowRuleNameUdp}\" dir=in action=allow protocol=UDP localport={newPort}");

            if (oldPort != newPort)
            {
                RunNetsh($"advfirewall firewall add rule name=\"OpenRdpGuard-Block-OldPort-{oldPort}\" dir=in action=block protocol=TCP localport={oldPort}");
                RunNetsh($"advfirewall firewall add rule name=\"OpenRdpGuard-Block-OldPort-UDP-{oldPort}\" dir=in action=block protocol=UDP localport={oldPort}");
            }
        }

        private void RunNetsh(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas"
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch { }
        }
    }
}
