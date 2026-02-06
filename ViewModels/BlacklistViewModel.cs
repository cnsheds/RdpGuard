using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Services;
using OpenRdpGuard.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public BlacklistViewModel(IFirewallService firewallService, ILogService logService, IConnectionService connectionService)
        {
            _firewallService = firewallService;
            _logService = logService;
            _connectionService = connectionService;
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Tick += OnMonitorTimerTick;
            UpdateMonitorInterval();
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
            var offenders = attempts.Where(a => !a.IsSuccess)
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() > 3)
                .Select(g => g.Key)
                .ToList();

            foreach (var ip in offenders)
            {
                if (_firewallService.IsLocalIp(ip)) continue;
                await _firewallService.BlockIpAsync(ip);
                await _connectionService.KillConnectionsByRemoteIpAsync(new[] { ip });
            }

            await RefreshList();
            _isScanRunning = false;
        }

        [RelayCommand]
        private async Task CheckBlacklistConnections()
        {
            var ips = await _firewallService.GetBlockedIpsAsync();
            await _connectionService.KillConnectionsByRemoteIpAsync(ips);
        }

        partial void OnIsMonitoringChanged(bool value)
        {
            if (value)
            {
                UpdateMonitorInterval();
                _monitorTimer.Start();
            }
            else
            {
                _monitorTimer.Stop();
            }
        }

        partial void OnMonitorIntervalMinutesChanged(int value)
        {
            if (value < 1)
            {
                MonitorIntervalMinutes = 1;
                return;
            }

            UpdateMonitorInterval();
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
