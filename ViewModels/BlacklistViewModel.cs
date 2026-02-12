using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using OpenRdpGuard.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OpenRdpGuard.ViewModels
{
    public partial class BlacklistViewModel : ObservableObject
    {
        private readonly IFirewallService _firewallService;
        private readonly ILogService _logService;
        private readonly IConnectionService _connectionService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly DispatcherTimer _monitorTimer;
        private bool _isScanRunning;

        [ObservableProperty]
        private ObservableCollection<string> _blockedIps = new();

        [ObservableProperty]
        private string _ipToAdd = string.Empty;

        [ObservableProperty]
        private ObservableCollection<int> _scanOptions = new() { 1, 6, 12, 24 };

        [ObservableProperty]
        private int _selectedScanHours = 24;

        [ObservableProperty]
        private string? _selectedBlockedIp;

        [ObservableProperty]
        private int _blockedCount;

        [ObservableProperty]
        private bool _isMonitoring;

        [ObservableProperty]
        private int _monitorIntervalMinutes = 10;

        [ObservableProperty]
        private string _monitoringSummary = string.Empty;

        [ObservableProperty]
        private bool _smartSubnetBlockingEnabled = true;

        public BlacklistViewModel(IFirewallService firewallService, ILogService logService, IConnectionService connectionService, IAppSettingsService appSettingsService)
        {
            _firewallService = firewallService;
            _logService = logService;
            _connectionService = connectionService;
            _appSettingsService = appSettingsService;
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Tick += OnMonitorTimerTick;
            LoadSettings();
            UpdateMonitoringSummary();
            _ = RefreshList();
        }

        [RelayCommand]
        private async Task RefreshList()
        {
            var ips = await _firewallService.GetBlockedIpsAsync();
            BlockedIps = new ObservableCollection<string>(ips);
            BlockedCount = BlockedIps.Count;
        }

        [RelayCommand]
        private async Task AddIp()
        {
            if (string.IsNullOrWhiteSpace(IpToAdd)) return;

            if (_firewallService.IsLocalIp(IpToAdd))
            {
                var dialog = new ConfirmDialog("提示", "为了防止误封，本机 IP 不允许加入黑名单。", showCancel: false);
                dialog.Owner = Application.Current?.MainWindow;
                dialog.ShowDialog();
                return;
            }

            var added = await _firewallService.BlockIpAsync(IpToAdd.Trim());
            if (added)
            {
                await _connectionService.KillConnectionsByRemoteIpAsync(new[] { IpToAdd.Trim() });
            }
            IpToAdd = string.Empty;
            await RefreshList();
        }

        [RelayCommand]
        private async Task RemoveSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedBlockedIp)) return;
            await _firewallService.UnblockIpAsync(SelectedBlockedIp);
            await RefreshList();
        }

        [RelayCommand]
        private async Task ScanAndBlock()
        {
            if (_isScanRunning) return;
            _isScanRunning = true;
            var attempts = await _logService.GetLoginAttemptsAsync(TimeSpan.FromHours(SelectedScanHours));
            if (!string.IsNullOrWhiteSpace(_logService.LastError))
            {
                _isScanRunning = false;
                var dialog = new ConfirmDialog("提示", "未以管理员运行，将无法读取 Security 日志中的失败登录记录。", showCancel: false);
                dialog.Owner = Application.Current?.MainWindow;
                dialog.ShowDialog();
                return;
            }
            var successCounts = attempts.Where(a => a.IsSuccess)
                .GroupBy(a => a.IpAddress)
                .ToDictionary(g => g.Key, g => g.Count());

            var failedAttempts = attempts.Where(a => !a.IsSuccess).ToList();
            var offenders = failedAttempts
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() > 3)
                .Where(g => !successCounts.TryGetValue(g.Key, out var successCount) || successCount <= 1)
                .Select(g => g.Key)
                .ToList();

            var rangeOffenders = new List<(string Cidr, List<string> Ips)>();
            if (SmartSubnetBlockingEnabled)
            {
                var successPrefix16Set = attempts
                    .Where(a => a.IsSuccess)
                    .Select(a => TryGetIpv4Prefix16(a.IpAddress))
                    .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                    .ToHashSet(StringComparer.Ordinal);

                var suspiciousFailedRanges = failedAttempts
                    .Select(a =>
                    {
                        var prefix16 = TryGetIpv4Prefix16(a.IpAddress);
                        var prefix24 = TryGetIpv4Prefix24(a.IpAddress);
                        return new { a.IpAddress, Prefix16 = prefix16, Prefix24 = prefix24 };
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Prefix16) && !string.IsNullOrWhiteSpace(x.Prefix24))
                    .GroupBy(x => new { x.Prefix16, x.Prefix24 })
                    .Where(g => g.Select(x => x.IpAddress).Distinct(StringComparer.Ordinal).Count() > 3)
                    .Where(g => !successPrefix16Set.Contains(g.Key.Prefix16!))
                    .ToList();

                rangeOffenders = suspiciousFailedRanges
                    .Select(g => (
                        Cidr: $"{g.Key.Prefix24}.0/24",
                        Ips: g.Select(x => x.IpAddress).Distinct(StringComparer.Ordinal).ToList()
                    ))
                    .ToList();
            }

            foreach (var ip in offenders)
            {
                if (_firewallService.IsLocalIp(ip)) continue;
                await _firewallService.BlockIpAsync(ip);
                await _connectionService.KillConnectionsByRemoteIpAsync(new[] { ip });
            }

            foreach (var range in rangeOffenders)
            {
                await _firewallService.BlockIpAsync(range.Cidr);
                if (range.Ips.Count > 0)
                {
                    await _connectionService.KillConnectionsByRemoteIpAsync(range.Ips);
                }
            }

            await RefreshList();
            _isScanRunning = false;
        }

        private static string? TryGetIpv4Prefix16(string ip)
        {
            if (!TryParseIpv4(ip, out var bytes))
            {
                return null;
            }

            return $"{bytes[0]}.{bytes[1]}";
        }

        private static string? TryGetIpv4Prefix24(string ip)
        {
            if (!TryParseIpv4(ip, out var bytes))
            {
                return null;
            }

            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}";
        }

        private static bool TryParseIpv4(string ip, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (!IPAddress.TryParse(ip, out var address) || address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            bytes = address.GetAddressBytes();
            return bytes.Length == 4;
        }

        [RelayCommand]
        private async Task CheckBlacklistConnections()
        {
            var ips = await _firewallService.GetBlockedIpsAsync();
            await _connectionService.KillConnectionsByRemoteIpAsync(ips);
        }

        private void LoadSettings()
        {
            var savedHours = _appSettingsService.GetBlacklistScanHours();
            if (ScanOptions.Contains(savedHours))
            {
                SelectedScanHours = savedHours;
            }

            MonitorIntervalMinutes = Math.Max(1, _appSettingsService.GetBlacklistMonitorIntervalMinutes());
            UpdateMonitorInterval();

            IsMonitoring = _appSettingsService.GetBlacklistMonitoringEnabled();
            if (IsMonitoring)
            {
                _monitorTimer.Start();
            }

            SmartSubnetBlockingEnabled = _appSettingsService.GetBlacklistSmartSubnetBlockingEnabled();
        }

        partial void OnIsMonitoringChanged(bool value)
        {
            _appSettingsService.SetBlacklistMonitoringEnabled(value);
            if (value)
            {
                UpdateMonitorInterval();
                _monitorTimer.Start();
                _ = ScanAndBlock();
            }
            else
            {
                _monitorTimer.Stop();
            }
            UpdateMonitoringSummary();
        }

        partial void OnMonitorIntervalMinutesChanged(int value)
        {
            if (value < 1)
            {
                MonitorIntervalMinutes = 1;
                return;
            }

            _appSettingsService.SetBlacklistMonitorIntervalMinutes(value);
            UpdateMonitorInterval();
            UpdateMonitoringSummary();
        }

        partial void OnSelectedScanHoursChanged(int value)
        {
            _appSettingsService.SetBlacklistScanHours(value);
            UpdateMonitoringSummary();
        }

        partial void OnSmartSubnetBlockingEnabledChanged(bool value)
        {
            _appSettingsService.SetBlacklistSmartSubnetBlockingEnabled(value);
        }

        private void UpdateMonitoringSummary()
        {
            var status = IsMonitoring ? "监控开启" : "监控关闭";
            MonitoringSummary = $"已保存：范围 {SelectedScanHours} 小时，{status}，每 {MonitorIntervalMinutes} 分钟扫描一次";
        }

        private void UpdateMonitorInterval()
        {
            var minutes = Math.Max(1, MonitorIntervalMinutes);
            _monitorTimer.Interval = TimeSpan.FromMinutes(minutes);
        }

        private async void OnMonitorTimerTick(object? sender, EventArgs e)
        {
            if (_isScanRunning) return;
            await ScanAndBlock();
        }
    }
}
