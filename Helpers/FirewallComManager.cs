using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRdpGuard.Helpers
{
    public class FirewallComManager
    {
        private readonly string _ruleName;
        private readonly string? _tcpAllowRuleName;
        private readonly string? _udpAllowRuleName;
        private static readonly string[] RemoteDesktopGroupKeywords = { "Remote Desktop", "远程桌面" };

        public const int ProtocolAny = 256;
        public const int ProtocolTcp = 6;
        public const int ProtocolUdp = 17;
        public const int DirectionIn = 1;
        public const int ActionBlock = 0;
        public const int ActionAllow = 1;
        public const int ProfileAll = int.MaxValue;

        public FirewallComManager(string ruleName)
        {
            _ruleName = ruleName;
        }

        public FirewallComManager(string blockRuleName, string tcpAllowRuleName, string udpAllowRuleName)
        {
            _ruleName = blockRuleName;
            _tcpAllowRuleName = tcpAllowRuleName;
            _udpAllowRuleName = udpAllowRuleName;
        }

        public bool RuleExists()
        {
            return FindRule() != null;
        }

        public void DeleteRuleIfExists()
        {
            var policy = GetPolicy();
            var rule = FindRule();
            if (rule != null)
            {
                policy.Rules.Remove(rule.Name);
            }
        }

        public void UpsertBlockRule(string remoteAddresses)
        {
            var policy = GetPolicy();
            var rule = FindRule();
            if (rule == null)
            {
                rule = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")!);
                rule.Name = _ruleName;
                rule.Direction = DirectionIn;
                rule.Action = ActionBlock;
                rule.Enabled = true;
                rule.Protocol = ProtocolAny;
                rule.Profiles = ProfileAll;
                policy.Rules.Add(rule);
            }

            rule.RemoteAddresses = remoteAddresses;
        }

        public List<string> GetRemoteAddresses()
        {
            var rule = FindRule();
            if (rule == null)
            {
                return new List<string>();
            }

            var raw = (string)(rule.RemoteAddresses ?? string.Empty);
            return SplitRemoteAddresses(raw);
        }

        public void DeleteAllowRules()
        {
            if (string.IsNullOrWhiteSpace(_tcpAllowRuleName) || string.IsNullOrWhiteSpace(_udpAllowRuleName))
            {
                return;
            }

            DeleteRuleIfExists(_tcpAllowRuleName);
            DeleteRuleIfExists(_udpAllowRuleName);
        }

        public void UpsertAllowRule(string ruleName, int protocol, int port, string remoteAddresses, bool enabled)
        {
            var policy = GetPolicy();
            var rule = FindRule(ruleName);
            if (rule == null)
            {
                rule = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")!);
                rule.Name = ruleName;
                rule.Direction = DirectionIn;
                rule.Action = ActionAllow;
                rule.Enabled = true;
                rule.Protocol = protocol;
                rule.Profiles = ProfileAll;
                policy.Rules.Add(rule);
            }

            rule.Protocol = protocol;
            rule.LocalPorts = port.ToString();
            rule.RemoteAddresses = NormalizeRemoteAddresses(remoteAddresses);
            rule.Enabled = enabled;
        }

        public void UpsertBlockRule(string ruleName, int protocol, int port)
        {
            var policy = GetPolicy();
            var rule = FindRule(ruleName);
            if (rule == null)
            {
                rule = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")!);
                rule.Name = ruleName;
                rule.Direction = DirectionIn;
                rule.Action = ActionBlock;
                rule.Enabled = true;
                rule.Protocol = protocol;
                rule.Profiles = ProfileAll;
                policy.Rules.Add(rule);
            }

            rule.Protocol = protocol;
            rule.LocalPorts = port.ToString();
            rule.Enabled = true;
        }

        public List<string> GetRemoteAddressesFromTcpAllowRule()
        {
            if (string.IsNullOrWhiteSpace(_tcpAllowRuleName))
            {
                return new List<string>();
            }

            var rule = FindRule(_tcpAllowRuleName);
            if (rule == null)
            {
                return new List<string>();
            }

            var raw = (string)(rule.RemoteAddresses ?? string.Empty);
            return SplitRemoteAddresses(raw);
        }

        public void SetRemoteDesktopRulesEnabled(bool enabled)
        {
            var policy = GetPolicy();
            foreach (var rule in policy.Rules)
            {
                if (IsRemoteDesktopRule(rule))
                {
                    rule.Enabled = enabled;
                }
            }
        }

        public void SetAllowRulesEnabled(bool enabled)
        {
            if (string.IsNullOrWhiteSpace(_tcpAllowRuleName) || string.IsNullOrWhiteSpace(_udpAllowRuleName))
            {
                return;
            }

            SetRuleEnabled(_tcpAllowRuleName, enabled);
            SetRuleEnabled(_udpAllowRuleName, enabled);
        }

        public bool AreAllowRulesEnabled()
        {
            if (string.IsNullOrWhiteSpace(_tcpAllowRuleName) || string.IsNullOrWhiteSpace(_udpAllowRuleName))
            {
                return false;
            }

            var tcp = FindRule(_tcpAllowRuleName);
            var udp = FindRule(_udpAllowRuleName);
            if (tcp == null || udp == null)
            {
                return false;
            }

            return (bool)tcp.Enabled && (bool)udp.Enabled;
        }

        public void DeleteRulesByPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return;
            }

            var policy = GetPolicy();
            var targets = new List<string>();
            foreach (var rule in policy.Rules)
            {
                var name = rule.Name as string;
                if (!string.IsNullOrWhiteSpace(name) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add(name);
                }
            }

            foreach (var name in targets)
            {
                policy.Rules.Remove(name);
            }
        }

        private dynamic? FindRule()
        {
            var policy = GetPolicy();
            foreach (var rule in policy.Rules)
            {
                var name = rule.Name as string;
                if (!string.IsNullOrWhiteSpace(name) && string.Equals(name, _ruleName, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }

            return null;
        }

        private dynamic? FindRule(string ruleName)
        {
            var policy = GetPolicy();
            foreach (var rule in policy.Rules)
            {
                var name = rule.Name as string;
                if (!string.IsNullOrWhiteSpace(name) && string.Equals(name, ruleName, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }

            return null;
        }

        private void DeleteRuleIfExists(string ruleName)
        {
            var policy = GetPolicy();
            var rule = FindRule(ruleName);
            if (rule != null)
            {
                policy.Rules.Remove(rule.Name);
            }
        }

        private void SetRuleEnabled(string ruleName, bool enabled)
        {
            var rule = FindRule(ruleName);
            if (rule != null)
            {
                rule.Enabled = enabled;
            }
        }

        private static bool IsRemoteDesktopRule(dynamic rule)
        {
            var serviceName = rule.ServiceName as string ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(serviceName) &&
                string.Equals(serviceName, "TermService", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var grouping = rule.Grouping as string ?? string.Empty;
            return RemoteDesktopGroupKeywords.Any(k => grouping.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static dynamic GetPolicy()
        {
            return Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!);
        }

        private static string NormalizeRemoteAddresses(string remoteAddresses)
        {
            if (string.IsNullOrWhiteSpace(remoteAddresses))
            {
                return "*";
            }

            if (string.Equals(remoteAddresses, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return "*";
            }

            return remoteAddresses;
        }

        private static List<string> SplitRemoteAddresses(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            return raw
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip) && !ip.Equals("Any", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
