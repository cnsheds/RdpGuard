using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface IWhitelistService
    {
        Task AllowIpAsync(string ip);
        Task RemoveIpAsync(string ip);
        Task<List<string>> GetAllowedIpsAsync();
        bool IsWhitelistEnabled();
        Task SetWhitelistEnabledAsync(bool enabled);
        Task ApplyWhitelistRulesAsync();
    }

    public class WhitelistService : IWhitelistService
    {
        private const string ConfigFile = "allowed_ips.json";
        private const string RuleAllowName = "OpenRdpGuardWhitelistAllow";
        private const string RuleAllowNameUdp = "OpenRdpGuardWhitelistAllowUdp";
        private List<string> _allowedIps = new();
        private readonly ISettingsService _settingsService;
        private readonly IAppSettingsService _appSettings;

        public WhitelistService(ISettingsService settingsService, IAppSettingsService appSettings)
        {
            _settingsService = settingsService;
            _appSettings = appSettings;
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFile);
                    _allowedIps = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                catch { }
            }
        }

        private void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_allowedIps, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch { }
        }

        public async Task AllowIpAsync(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip) && !_allowedIps.Contains(ip))
            {
                _allowedIps.Add(ip);
                SaveConfig();
            }
            await ApplyWhitelistRulesAsync();
        }

        public async Task RemoveIpAsync(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip) && _allowedIps.Contains(ip))
            {
                _allowedIps.Remove(ip);
                SaveConfig();
            }
            await ApplyWhitelistRulesAsync();
        }

        public Task<List<string>> GetAllowedIpsAsync()
        {
            return Task.FromResult(_allowedIps.ToList());
        }

        public bool IsWhitelistEnabled()
        {
            return _appSettings.GetWhitelistEnabled();
        }

        public async Task SetWhitelistEnabledAsync(bool enabled)
        {
            _appSettings.SetWhitelistEnabled(enabled);
            await ApplyWhitelistRulesAsync();
        }

        public async Task ApplyWhitelistRulesAsync()
        {
            await Task.Run(() =>
            {
                var enabled = _appSettings.GetWhitelistEnabled();
                var port = _settingsService.GetRdpPort();
                var ipList = string.Join(",", _allowedIps);

                RunNetsh($"advfirewall firewall delete rule name=\"{RuleAllowName}\"");
                RunNetsh($"advfirewall firewall delete rule name=\"{RuleAllowNameUdp}\"");

                if (!enabled || _allowedIps.Count == 0)
                {
                    RunNetsh("advfirewall firewall set rule group=\"remote desktop\" new enable=yes");
                    return;
                }

                RunNetsh("advfirewall firewall set rule group=\"remote desktop\" new enable=no");
                RunNetsh($"advfirewall firewall add rule name=\"{RuleAllowName}\" dir=in action=allow protocol=TCP localport={port} remoteip=\"{ipList}\"");
                RunNetsh($"advfirewall firewall add rule name=\"{RuleAllowNameUdp}\" dir=in action=allow protocol=UDP localport={port} remoteip=\"{ipList}\"");
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
            catch
            {
                return (-1, string.Empty);
            }
        }
    }
}
