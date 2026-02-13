using OpenRdpGuard.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenRdpGuard.Services
{
    public interface IShareService
    {
        Task<List<ShareEntry>> GetSharesAsync();
        Task<(bool Success, string Message)> DeleteShareAsync(string name);
        Task<bool> GetAutoAdminShareEnabledAsync();
        Task<(bool Success, string Message)> SetAutoAdminShareEnabledAsync(bool enabled);
    }

    public class ShareService : IShareService
    {
        private const string LanmanParamsKey = "HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters";

        public Task<List<ShareEntry>> GetSharesAsync()
        {
            return Task.Run(() =>
            {
                var shares = new List<ShareEntry>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, Path, Description FROM Win32_Share");
                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        var name = (obj["Name"] as string ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        var path = (obj["Path"] as string ?? string.Empty).Trim();
                        var remark = (obj["Description"] as string ?? string.Empty).Trim();

                        var upper = name.ToUpperInvariant();
                        var isSystem = upper == "IPC$";
                        var isAdmin = !isSystem && upper.EndsWith("$", StringComparison.Ordinal);

                        shares.Add(new ShareEntry
                        {
                            Name = name,
                            Path = path,
                            Remark = remark,
                            IsSystemShare = isSystem,
                            IsAdminShare = isAdmin,
                            IsCustomShare = !isSystem && !isAdmin
                        });
                    }
                }
                catch
                {
                    // Keep UI stable when WMI is unavailable.
                    return new List<ShareEntry>();
                }

                return shares
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            });
        }

        public async Task<(bool Success, string Message)> DeleteShareAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "共享名为空");
            }

            var result = await RunProcessAsync("cmd.exe", $"/c net share \"{name}\" /delete");
            if (result.ExitCode == 0)
            {
                return (true, $"已删除共享：{name}");
            }

            var message = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error;
            return (false, string.IsNullOrWhiteSpace(message) ? "删除共享失败，请确认管理员权限" : message.Trim());
        }

        public async Task<bool> GetAutoAdminShareEnabledAsync()
        {
            var wks = await ReadRegDwordAsync("AutoShareWks");
            var server = await ReadRegDwordAsync("AutoShareServer");

            if ((wks.HasValue && wks.Value == 0) || (server.HasValue && server.Value == 0))
            {
                return false;
            }

            return true;
        }

        public async Task<(bool Success, string Message)> SetAutoAdminShareEnabledAsync(bool enabled)
        {
            var value = enabled ? 1 : 0;
            var cmd = $"/c reg add \"{LanmanParamsKey}\" /v AutoShareWks /t REG_DWORD /d {value} /f && reg add \"{LanmanParamsKey}\" /v AutoShareServer /t REG_DWORD /d {value} /f";
            var result = await RunProcessAsync("cmd.exe", cmd);

            if (result.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error;
                return (false, string.IsNullOrWhiteSpace(message) ? "更新默认共享策略失败，请确认管理员权限" : message.Trim());
            }

            if (enabled)
            {
                return (true, "已启用默认管理共享（重启服务或系统后完全生效）");
            }

            var removed = await RemoveCurrentAdminSharesAsync();
            return (true, $"已禁用默认管理共享，当前会话已移除 {removed} 个默认共享（重启服务或系统后完全生效）");
        }

        private async Task<int> RemoveCurrentAdminSharesAsync()
        {
            var shares = await GetSharesAsync();
            var targets = shares
                .Select(s => s.Name)
                .Where(IsImmediateDisableTarget)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var removed = 0;
            foreach (var name in targets)
            {
                var result = await RunProcessAsync("cmd.exe", $"/c net share \"{name}\" /delete");
                if (result.ExitCode == 0)
                {
                    removed++;
                }
            }

            return removed;
        }

        private static bool IsImmediateDisableTarget(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var upper = name.Trim().ToUpperInvariant();
            if (upper == "ADMIN$")
            {
                return true;
            }

            return Regex.IsMatch(upper, "^[A-Z]\\$$");
        }

        private static async Task<int?> ReadRegDwordAsync(string name)
        {
            var result = await RunProcessAsync("cmd.exe", $"/c reg query \"{LanmanParamsKey}\" /v {name}");
            if (result.ExitCode != 0)
            {
                return null;
            }

            var match = Regex.Match(result.Output, @"REG_DWORD\s+0x([0-9a-fA-F]+)");
            if (!match.Success)
            {
                return null;
            }

            return int.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out var value)
                ? value
                : null;
        }

        private static Task<(int ExitCode, string Output, string Error)> RunProcessAsync(string fileName, string arguments)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo(fileName, arguments)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p == null)
                    {
                        return (-1, string.Empty, "无法启动进程");
                    }

                    var output = p.StandardOutput.ReadToEnd();
                    var error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return (p.ExitCode, output, error);
                }
                catch (Exception ex)
                {
                    return (-1, string.Empty, ex.Message);
                }
            });
        }
    }
}
