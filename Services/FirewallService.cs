using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface IFirewallService
    {
        Task<bool> BlockIpAsync(string ip);
        Task<bool> UnblockIpAsync(string ip);
        Task<List<string>> GetBlockedIpsAsync();
        bool IsLocalIp(string ip);
        bool IsManaged();
    }

    public class FirewallService : IFirewallService
    {
        private const string RuleName = "OpenRdpGuardBlock";
        private const string ConfigFile = "blocked_ips.json";
        private List<string> _blockedIps = new();

        public FirewallService()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFile);
                    _blockedIps = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                catch { }
            }
        }

        private void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_blockedIps, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch { }
        }

        public bool IsLocalIp(string ip)
        {
            if (!IPAddress.TryParse(ip, out var target))
            {
                return false;
            }

            try
            {
                var localAddresses = NetworkInterface.GetAllNetworkInterfaces()
                    .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                    .Select(ua => ua.Address)
                    .ToList();

                return localAddresses.Any(addr => addr.Equals(target));
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BlockIpAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            if (IsLocalIp(ip))
            {
                return false;
            }

            if (!_blockedIps.Contains(ip))
            {
                _blockedIps.Add(ip);
                SaveConfig();
                await SyncFirewallRule();
                return true;
            }

            return false;
        }

        public async Task<bool> UnblockIpAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            if (_blockedIps.Contains(ip))
            {
                _blockedIps.Remove(ip);
                SaveConfig();
                await SyncFirewallRule();
                return true;
            }

            return false;
        }

        public Task<List<string>> GetBlockedIpsAsync()
        {
            return Task.FromResult(_blockedIps.ToList());
        }

        public bool IsManaged()
        {
            try
            {
                var result = RunNetsh($"advfirewall firewall show rule name=\"{RuleName}\"");
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task SyncFirewallRule()
        {
            await Task.Run(() =>
            {
                if (_blockedIps.Count == 0)
                {
                    RunNetsh($"advfirewall firewall delete rule name=\"{RuleName}\"");
                    return;
                }

                var ipList = string.Join(",", _blockedIps);

                bool exists = RunNetsh($"advfirewall firewall show rule name=\"{RuleName}\"").ExitCode == 0;

                if (exists)
                {
                    RunNetsh($"advfirewall firewall set rule name=\"{RuleName}\" new remoteip=\"{ipList}\"");
                }
                else
                {
                    RunNetsh($"advfirewall firewall add rule name=\"{RuleName}\" dir=in action=block protocol=any remoteip=\"{ipList}\"");
                }
            });
        }

        private (int ExitCode, string Output) RunNetsh(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                var p = Process.Start(psi);
                p.WaitForExit();
                return (p.ExitCode, p.StandardOutput.ReadToEnd());
            }
            catch (Exception ex)
            {
                return (-1, ex.Message);
            }
        }
    }
}
