using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public class ConnectionInfo
    {
        public string Proto { get; set; } = string.Empty;
        public string LocalAddress { get; set; } = string.Empty;
        public string RemoteAddress { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Pid { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool IsBlacklisted { get; set; }
    }

    public interface IConnectionService
    {
        Task<List<ConnectionInfo>> GetActiveConnectionsAsync();
        Task KillConnectionAsync(int pid);
        Task<int> KillConnectionsByRemoteIpAsync(IEnumerable<string> ips);
    }

    public class ConnectionService : IConnectionService
    {
        public async Task<List<ConnectionInfo>> GetActiveConnectionsAsync()
        {
            return await Task.Run(() =>
            {
                var connections = new List<ConnectionInfo>();
                try
                {
                    var psi = new ProcessStartInfo("netstat", "-ano")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p == null) return connections;

                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = Regex.Split(line.Trim(), @"\s+");
                        if (parts.Length < 4) continue;

                        if (parts[0].StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                        {
                            if (parts.Length < 5) continue;

                            var conn = new ConnectionInfo
                            {
                                Proto = parts[0],
                                LocalAddress = parts[1],
                                RemoteAddress = parts[2],
                                State = parts[3],
                                Pid = int.TryParse(parts[4], out var pid) ? pid : 0
                            };

                            if (conn.State == "ESTABLISHED" || conn.LocalAddress.EndsWith(":3389", StringComparison.OrdinalIgnoreCase))
                            {
                                try { conn.ProcessName = Process.GetProcessById(conn.Pid).ProcessName; } catch { }
                                connections.Add(conn);
                            }
                        }
                        else if (parts[0].StartsWith("UDP", StringComparison.OrdinalIgnoreCase))
                        {
                            var conn = new ConnectionInfo
                            {
                                Proto = parts[0],
                                LocalAddress = parts[1],
                                RemoteAddress = parts.Length >= 3 ? parts[2] : "*:*",
                                State = "",
                                Pid = parts.Length >= 4 && int.TryParse(parts[3], out var pid) ? pid : 0
                            };

                            try { conn.ProcessName = Process.GetProcessById(conn.Pid).ProcessName; } catch { }
                            connections.Add(conn);
                        }
                    }
                }
                catch { }
                return connections;
            });
        }

        public async Task KillConnectionAsync(int pid)
        {
            await Task.Run(() =>
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    p.Kill();
                }
                catch { }
            });
        }

        public async Task<int> KillConnectionsByRemoteIpAsync(IEnumerable<string> ips)
        {
            if (ips == null) return 0;

            var ipSet = new HashSet<string>(ips.Where(ip => !string.IsNullOrWhiteSpace(ip)));
            if (ipSet.Count == 0) return 0;

            var killed = 0;
            var conns = await GetActiveConnectionsAsync();
            foreach (var conn in conns)
            {
                var remote = conn.RemoteAddress.Split(':')[0];
                if (ipSet.Contains(remote))
                {
                    try
                    {
                        var p = Process.GetProcessById(conn.Pid);
                        p.Kill();
                        killed++;
                    }
                    catch { }
                }
            }

            return killed;
        }
    }
}
