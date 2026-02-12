using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenRdpGuard.Models;
using OpenRdpGuard.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OpenRdpGuard.ViewModels
{
    public partial class LoginStatsViewModel : ObservableObject
    {
        private readonly ILogService _logService;

        [ObservableProperty]
        private ObservableCollection<string> _rangeOptions = new() { "1小时", "24小时", "72小时", "168小时" };

        [ObservableProperty]
        private string _selectedRange = "24小时";

        [ObservableProperty]
        private int _failedCount;

        [ObservableProperty]
        private int _successCount;

        [ObservableProperty]
        private ObservableCollection<StatItem> _topFailedIps = new();

        [ObservableProperty]
        private ObservableCollection<StatItem> _topSuccessIps = new();

        [ObservableProperty]
        private ObservableCollection<StatItem> _topFailedUsers = new();

        [ObservableProperty]
        private string _warningMessage = string.Empty;

        [ObservableProperty]
        private bool _showWarning;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private bool _isNotScanning = true;

        public LoginStatsViewModel(ILogService logService)
        {
            _logService = logService;
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsScanning = true;
            var hours = ParseHours(SelectedRange);
            var attempts = await _logService.GetLoginAttemptsAsync(TimeSpan.FromHours(hours));
            if (!string.IsNullOrWhiteSpace(_logService.LastError))
            {
                WarningMessage = "未以管理员运行，将无法读取 Security 日志中的失败登录记录";
                ShowWarning = true;
            }
            else
            {
                WarningMessage = string.Empty;
                ShowWarning = false;
            }
            var failed = attempts.Where(a => !a.IsSuccess).ToList();
            var success = attempts.Where(a => a.IsSuccess).ToList();

            FailedCount = failed.Count;
            SuccessCount = success.Count;

            TopFailedIps = new ObservableCollection<StatItem>(
                failed.GroupBy(a => a.IpAddress)
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .Select(g => new StatItem { Label = g.Key, Count = g.Count() })
            );

            TopSuccessIps = new ObservableCollection<StatItem>(
                success.GroupBy(a => a.IpAddress)
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .Select(g => new StatItem { Label = g.Key, Count = g.Count() })
            );

            TopFailedUsers = new ObservableCollection<StatItem>(
                failed.Where(a => !string.IsNullOrWhiteSpace(a.Username))
                    .GroupBy(a => a.Username)
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .Select(g => new StatItem { Label = g.Key, Count = g.Count() })
            );
            IsScanning = false;
        }

        partial void OnIsScanningChanged(bool value)
        {
            IsNotScanning = !value;
        }

        private static int ParseHours(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 24;
            var trimmed = value.Replace("小时", string.Empty).Trim();
            return int.TryParse(trimmed, out var hours) ? hours : 24;
        }
    }
}
