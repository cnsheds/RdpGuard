using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using OpenRdpGuard.Helpers;

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
        private const string RuleName = FirewallRuleNames.BlockRule;
        private List<string> _blockedIps = new();
        private readonly FirewallComManager _comManager = new FirewallComManager(RuleName);

        public FirewallService()
        {
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
                await SyncFirewallRule();
                return true;
            }

            return false;
        }

        public bool IsManaged()
        {
            try
            {
                return _comManager.RuleExists();
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
                    _comManager.DeleteRuleIfExists();
                    return;
                }

                var ipList = string.Join(",", _blockedIps);

                _comManager.UpsertBlockRule(ipList);
            });
        }

        public Task<List<string>> GetBlockedIpsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _blockedIps = _comManager.GetRemoteAddresses();
                    return _blockedIps.ToList();
                }
                catch
                {
                    return new List<string>();
                }
            });
        }
    }
}
