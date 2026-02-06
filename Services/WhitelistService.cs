using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenRdpGuard.Helpers;

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
        private const string RuleAllowName = FirewallRuleNames.AllowRule;
        private const string RuleAllowNameUdp = FirewallRuleNames.AllowRuleUdp;
        private List<string> _allowedIps = new();
        private readonly ISettingsService _settingsService;
        private readonly IAppSettingsService _appSettings;
        private readonly FirewallComManager _comManager;

        public WhitelistService(ISettingsService settingsService, IAppSettingsService appSettings)
        {
            _settingsService = settingsService;
            _appSettings = appSettings;
            _comManager = new FirewallComManager(FirewallRuleNames.BlockRule, RuleAllowName, RuleAllowNameUdp);
            _allowedIps = _comManager.GetRemoteAddressesFromTcpAllowRule();
        }

        public async Task AllowIpAsync(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip) && !_allowedIps.Contains(ip))
            {
                _allowedIps.Add(ip);
            }
            await ApplyWhitelistRulesAsync();
        }

        public async Task RemoveIpAsync(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip) && _allowedIps.Contains(ip))
            {
                _allowedIps.Remove(ip);
            }
            await ApplyWhitelistRulesAsync();
        }

        public Task<List<string>> GetAllowedIpsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _allowedIps = _comManager.GetRemoteAddressesFromTcpAllowRule();
                    return _allowedIps.ToList();
                }
                catch
                {
                    return new List<string>();
                }
            });
        }

        public bool IsWhitelistEnabled()
        {
            var settingsEnabled = _appSettings.GetWhitelistEnabled();
            if (!settingsEnabled)
            {
                return false;
            }

            try
            {
                return _comManager.AreAllowRulesEnabled();
            }
            catch
            {
                return false;
            }
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

                if (!enabled || _allowedIps.Count == 0)
                {
                    _comManager.SetRemoteDesktopRulesEnabled(true);
                    _comManager.UpsertAllowRule(RuleAllowName, FirewallComManager.ProtocolTcp, port, "Any", true);
                    _comManager.UpsertAllowRule(RuleAllowNameUdp, FirewallComManager.ProtocolUdp, port, "Any", true);
                    _comManager.SetAllowRulesEnabled(true);
                    return;
                }

                _comManager.SetRemoteDesktopRulesEnabled(false);
                _comManager.UpsertAllowRule(RuleAllowName, FirewallComManager.ProtocolTcp, port, ipList, true);
                _comManager.UpsertAllowRule(RuleAllowNameUdp, FirewallComManager.ProtocolUdp, port, ipList, true);
                _comManager.SetAllowRulesEnabled(true);
            });
        }
    }
}
